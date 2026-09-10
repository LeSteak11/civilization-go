using System;
using System.Collections.Generic;

namespace Epoch.Application.Progression
{
    public sealed record CapitalDefinition(
        string CapitalId,
        IReadOnlyList<int> StageCosts,
        int CompletionReward);

    public static class ProgressionCatalog
    {
        public static readonly IReadOnlyList<string> LandmarkIds = new[]
        {
            "landmark_01",
            "landmark_02",
            "landmark_03",
            "landmark_04",
            "landmark_05",
        };

        public static readonly IReadOnlyList<CapitalDefinition> Capitals = new[]
        {
            new CapitalDefinition("capital_01", new[] { 20, 30, 40, 50, 60 }, 250),
            new CapitalDefinition("capital_02", new[] { 30, 50, 70, 90, 110 }, 350),
            new CapitalDefinition("capital_03", new[] { 50, 75, 100, 125, 150 }, 500),
        };

        public static CapitalDefinition Capital(string capitalId)
        {
            for (int i = 0; i < Capitals.Count; i++)
            {
                if (string.Equals(Capitals[i].CapitalId, capitalId, StringComparison.Ordinal))
                {
                    return Capitals[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(capitalId), "Unknown capital '" + capitalId + "'.");
        }

        public static int CapitalIndex(string capitalId)
        {
            for (int i = 0; i < Capitals.Count; i++)
            {
                if (string.Equals(Capitals[i].CapitalId, capitalId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LandmarkIndex(string landmarkId)
        {
            for (int i = 0; i < LandmarkIds.Count; i++)
            {
                if (string.Equals(LandmarkIds[i], landmarkId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        public static ProgressionState NewState()
        {
            List<CapitalProgress> capitals = new List<CapitalProgress>(Capitals.Count);
            for (int i = 0; i < Capitals.Count; i++)
            {
                List<LandmarkProgress> landmarks = new List<LandmarkProgress>(LandmarkIds.Count);
                for (int j = 0; j < LandmarkIds.Count; j++)
                {
                    landmarks.Add(new LandmarkProgress(LandmarkIds[j], 0));
                }

                capitals.Add(new CapitalProgress(
                    Capitals[i].CapitalId,
                    i == 0,
                    false,
                    false,
                    landmarks));
            }

            return new ProgressionState(
                ProgressionState.CurrentSchemaVersion,
                0,
                Capitals[0].CapitalId,
                capitals,
                Array.Empty<BattleRunProgress>());
        }
    }
}
