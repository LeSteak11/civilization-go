using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Epoch.Application.Progression
{
    public interface ProgressionStore
    {
        string? Load();

        void Save(string serializedState);
    }

    public sealed class FileProgressionStore : ProgressionStore
    {
        private readonly string _path;

        public FileProgressionStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A progression save path is required.", nameof(path));
            }

            _path = path;
        }

        public string? Load() => File.Exists(_path) ? File.ReadAllText(_path) : null;

        public void Save(string serializedState) => File.WriteAllText(_path, serializedState);
    }

    public static class ProgressionSaveCodec
    {
        private const string Header = "EPOCH-PROGRESSION|1";

        public static string Encode(ProgressionState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            StringBuilder text = new StringBuilder();
            text.AppendLine(Header);
            text.Append("GOLD|").Append(state.Gold.ToString(CultureInfo.InvariantCulture)).AppendLine();
            text.Append("CURRENT|").Append(Escape(state.CurrentCapitalId)).AppendLine();

            for (int i = 0; i < state.Capitals.Count; i++)
            {
                CapitalProgress capital = state.Capitals[i];
                text.Append("CAPITAL|")
                    .Append(Escape(capital.CapitalId)).Append('|')
                    .Append(Bit(capital.IsUnlocked)).Append('|')
                    .Append(Bit(capital.IsCompleted)).Append('|')
                    .Append(Bit(capital.CompletionRewardClaimed)).Append('|');
                for (int j = 0; j < capital.Landmarks.Count; j++)
                {
                    if (j > 0)
                    {
                        text.Append(',');
                    }

                    text.Append(capital.Landmarks[j].Stage.ToString(CultureInfo.InvariantCulture));
                }

                text.AppendLine();
            }

            for (int i = 0; i < state.BattleRuns.Count; i++)
            {
                BattleRunProgress run = state.BattleRuns[i];
                text.Append("BATTLE|")
                    .Append(Escape(run.BattleRunId)).Append('|')
                    .Append(run.Eligibility.ToString()).Append('|')
                    .Append(Bit(run.IsCompleted)).Append('|')
                    .Append(Bit(run.RewardGranted)).Append('|')
                    .Append(run.GoldAwarded.ToString(CultureInfo.InvariantCulture))
                    .AppendLine();
            }

            return text.ToString();
        }

        public static ProgressionState Decode(string serializedState)
        {
            if (serializedState is null)
            {
                throw new ArgumentNullException(nameof(serializedState));
            }

            string[] lines = serializedState.Replace("\r", string.Empty, StringComparison.Ordinal)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3 || !string.Equals(lines[0], Header, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Unsupported progression save schema.");
            }

            int? gold = null;
            string? current = null;
            List<CapitalProgress> capitals = new List<CapitalProgress>();
            List<BattleRunProgress> battleRuns = new List<BattleRunProgress>();

            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split('|');
                switch (fields[0])
                {
                    case "GOLD" when fields.Length == 2:
                        gold = ParseNonNegative(fields[1], "Gold");
                        break;
                    case "CURRENT" when fields.Length == 2:
                        current = Unescape(fields[1]);
                        break;
                    case "CAPITAL" when fields.Length == 6:
                        capitals.Add(DecodeCapital(fields));
                        break;
                    case "BATTLE" when fields.Length == 6:
                        battleRuns.Add(DecodeBattle(fields));
                        break;
                    default:
                        throw new InvalidDataException("Malformed progression save line " + (i + 1) + ".");
                }
            }

            if (gold is null || current is null)
            {
                throw new InvalidDataException("Progression save is missing Gold or current capital.");
            }

            ProgressionState state = new ProgressionState(
                ProgressionState.CurrentSchemaVersion,
                gold.Value,
                current,
                capitals,
                battleRuns);
            ProgressionService.Validate(state);
            return state;
        }

        private static CapitalProgress DecodeCapital(string[] fields)
        {
            string[] stages = fields[5].Split(',');
            if (stages.Length != ProgressionCatalog.LandmarkIds.Count)
            {
                throw new InvalidDataException("A capital save must contain five landmark stages.");
            }

            List<LandmarkProgress> landmarks = new List<LandmarkProgress>(stages.Length);
            for (int i = 0; i < stages.Length; i++)
            {
                int stage = ParseNonNegative(stages[i], "Landmark stage");
                if (stage > 5)
                {
                    throw new InvalidDataException("Landmark stage must be in 0..5.");
                }

                landmarks.Add(new LandmarkProgress(ProgressionCatalog.LandmarkIds[i], stage));
            }

            return new CapitalProgress(
                Unescape(fields[1]),
                ParseBit(fields[2]),
                ParseBit(fields[3]),
                ParseBit(fields[4]),
                landmarks);
        }

        private static BattleRunProgress DecodeBattle(string[] fields)
        {
            if (!Enum.TryParse(fields[2], false, out BattleRewardEligibility eligibility))
            {
                throw new InvalidDataException("Unknown battle reward eligibility.");
            }

            return new BattleRunProgress(
                Unescape(fields[1]),
                eligibility,
                ParseBit(fields[3]),
                ParseBit(fields[4]),
                ParseNonNegative(fields[5], "Battle Gold award"));
        }

        private static int ParseNonNegative(string text, string label)
        {
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < 0)
            {
                throw new InvalidDataException(label + " must be a non-negative integer.");
            }

            return value;
        }

        private static bool ParseBit(string text) => text switch
        {
            "0" => false,
            "1" => true,
            _ => throw new InvalidDataException("Boolean save fields must be 0 or 1."),
        };

        private static char Bit(bool value) => value ? '1' : '0';

        private static string Escape(string value) => Uri.EscapeDataString(value);

        private static string Unescape(string value) => Uri.UnescapeDataString(value);
    }
}
