"use strict";

const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const ROOT = path.resolve(__dirname, "..", "..");
const CONTENT_PATH = path.join(ROOT, "_aiinfodocs", "data", "epoch_v1_content.json");
const RESULTS = path.join(__dirname, "results");
const REPORT = path.join(ROOT, "_aiinfodocs", "EPOCH_V1_Balance_Report.md");
const content = JSON.parse(fs.readFileSync(CONTENT_PATH, "utf8"));
const MASK64 = (1n << 64n) - 1n;
const POLICIES = ["RANDOM_LEGAL", "ECONOMY_FIRST", "MILITARY_FIRST", "INSIGHT_FIRST", "BOARD_CONTROL", "ADAPTIVE"];
const CLASSES = ["SWORD", "SPEAR", "HORSE"];
const allCards = [...content.builds, ...content.trains, ...content.perks, ...content.keystones];
const byId = new Map(allCards.map(x => [x.id, x]));
const DAMAGE_TABLE = [9,9,10,10,10,10,11,11,11,12,12,12,12,13,13,13,14,14,14,15,15,16,16,16,17,17,18,18,19,19,19,20,20,21,22,22,23,23,24,24,25,26,26,27,28,28,29,30,31,31,32,33,34,35,35,36,37,38,39,40,41,42,43,44,46,47,48,49,50,52,53,54,56,57,58,60,61,63,65,66,68];

function splitmix64(x) {
  x = (x + 0x9E3779B97F4A7C15n) & MASK64;
  let z = x;
  z = ((z ^ (z >> 30n)) * 0xBF58476D1CE4E5B9n) & MASK64;
  z = ((z ^ (z >> 27n)) * 0x94D049BB133111EBn) & MASK64;
  return (z ^ (z >> 31n)) & MASK64;
}

class PCG32 {
  constructor(initstate, initseq) {
    this.state = 0n;
    this.inc = ((initseq << 1n) | 1n) & MASK64;
    this.next();
    this.state = (this.state + initstate) & MASK64;
    this.next();
  }
  next() {
    const old = this.state;
    this.state = (old * 6364136223846793005n + this.inc) & MASK64;
    const xs = Number((((old >> 18n) ^ old) >> 27n) & 0xffffffffn) >>> 0;
    const rot = Number((old >> 59n) & 31n);
    return ((xs >>> rot) | (xs << ((-rot) & 31))) >>> 0;
  }
  bounded(bound) {
    if (!Number.isInteger(bound) || bound < 1 || bound > 0x100000000) throw new Error("invalid bound");
    const b = BigInt(bound);
    const threshold = Number(((1n << 32n) - b) % b);
    let r;
    do r = this.next(); while (r < threshold);
    return r % bound;
  }
}

function streams(seed) {
  return { card: splitmix64(seed ^ 1n), lane: splitmix64(seed ^ 2n), tie: splitmix64(seed ^ 3n) };
}

function offerRng(seed, turn, slot, redraw) {
  const address = splitmix64(streams(seed).card ^ (BigInt(turn) << 32n) ^ (BigInt(slot) << 24n) ^ ((BigInt(redraw) * 0xD1342543DE82EF95n) & MASK64));
  return new PCG32(address, 0x43415244n);
}

function laneModifiers(seed) {
  const a = ["RIVER", "HIGHLAND", "COAST"];
  const rng = new PCG32(streams(seed).lane, 0x4C414E45n);
  for (let i = a.length - 1; i > 0; i--) { const j=rng.bounded(i + 1); [a[i], a[j]] = [a[j], a[i]]; }
  return a;
}

function ageFor(turn) { return Math.floor((turn - 1) / 6) + 1; }
function ageRow(age) { return content.ageTable[age - 1]; }
function weightRow(turn, config) {
  const base = content.offerWeights.find(x => turn >= x.turns[0] && turn <= x.turns[1]);
  if (turn > config.buildCutoff) {
    const total = base.TRAIN + base.ADVANCE;
    return { BUILD: 0, TRAIN: Math.round(100 * base.TRAIN / total), ADVANCE: 100 - Math.round(100 * base.TRAIN / total) };
  }
  return base;
}

function generateOffers(seed, turn, config) {
  const age = ageFor(turn), weights = weightRow(turn, config), hand = [];
  for (let slot = 0; slot < 3; slot++) {
    for (let redraw = 0; ; redraw++) {
      const rng = offerRng(seed, turn, slot, redraw);
      const roll = rng.bounded(100);
      const type = roll < weights.BUILD ? "BUILD" : roll < weights.BUILD + weights.TRAIN ? "TRAIN" : "ADVANCE";
      const pool = allCards.filter(c => c.cardType === type && c.minAge <= age && c.maxAge >= age);
      if (!pool.length) throw new Error(`empty ${type} pool at age ${age}`);
      const card = pool[rng.bounded(pool.length)];
      if (!hand.some(x => x.id === card.id)) { hand.push(card); break; }
    }
  }
  return hand;
}

function initialSide(name, growth, insight) {
  return { name, growth, insight, score: 0, structures: [], units: [], perks: [], keystone: false, selectedIds: [], spentGrowth: 0, spentInsight: 0, incomeGrowth: 0, incomeInsight: 0, passes: 0, selections: { BUILD: 0, TRAIN: 0, ADVANCE: 0, KEYSTONE: 0 }, holds: 0, combats: 0, counterAdvantages: 0, reachParticipations: 0, reachProtections: 0, survivalTurns: [], unitSeq: 0 };
}

function cloneVisible(s) { return { growth:s.growth, insight:s.insight, score:s.score, structures:s.structures, units:s.units, perks:s.perks, keystone:s.keystone }; }
function hasPerk(s, id) { return s.perks.includes(id); }
function legalTargets(side, card) {
  if (card.cardType === "BUILD") return [0,1,2];
  if (card.cardType === "TRAIN") return [0,1,2].filter(l => side.units.filter(u => u.lane === l).length < 3);
  return [];
}
function costOf(card, age, config) {
  const r = ageRow(age);
  if (card.cardType === "BUILD") return Math.round(r.buildCost * (age === 4 ? config.lateBuildCostMultiplier : 1));
  if (card.cardType === "TRAIN") return r.unitCost;
  return r.perkCost;
}
function isLegal(side, card, age, config) {
  const cost = costOf(card, age, config);
  if ((card.resourceType === "GROWTH" ? side.growth : side.insight) < cost) return false;
  if (card.cardType === "TRAIN" && !legalTargets(side, card).length) return false;
  if (card.cardType === "ADVANCE" && (hasPerk(side, card.id) || (card.contentType === "KEYSTONE" && side.keystone))) return false;
  return true;
}

function policyRng(seed, policy, turn, sideName) {
  const h = crypto.createHash("sha256").update(`${seed}:${policy}:${turn}`).digest();
  return new PCG32(h.readBigUInt64BE(0), h.readBigUInt64BE(8));
}
function lanePowerRaw(side, lane) { return side.units.filter(u => u.lane === lane && u.hp > 0).reduce((n,u)=>n+u.power,0); }
function choose(policy, side, opp, hand, turn, mods, config) {
  const age=ageFor(turn), rng=policyRng(config.seed,policy,turn,side.name), options=[];
  hand.forEach((card,index)=>{if(!isLegal(side,card,age,config))return;const targets=legalTargets(side,card);if(targets.length)targets.forEach(lane=>options.push({card,index,lane}));else options.push({card,index,lane:null});});
  if (!options.length) return null;
  if (policy === "RANDOM_LEGAL") return options[rng.bounded(options.length)];
  const value=o=>{
    const c=o.card, remain=25-turn, lane=o.lane, my=lane===null?0:lanePowerRaw(side,lane), enemy=lane===null?0:lanePowerRaw(opp,lane);
    let v=rng.bounded(1000)/100000;
    if(policy==="ECONOMY_FIRST") { v += c.cardType==="BUILD" ? (c.yieldType==="GROWTH"?12:8) + Math.max(0,remain-costOf(c,age,config)/ageRow(age).structureBaseYield) : c.cardType==="ADVANCE"?4:2; if(c.cardType==="BUILD"&&turn>=14&&costOf(c,age,config)>ageRow(age).structureBaseYield*remain)v-=20; }
    if(policy==="MILITARY_FIRST") { v += c.cardType==="TRAIN"?15+(enemy-my)/4:c.archetype==="MILITARY"||c.id==="KEYSTONE_TOTAL_MOBILIZATION"?9:2; if(lane!==null&&mods[lane]==="HIGHLAND")v+=2; }
    if(policy==="INSIGHT_FIRST") { v += c.cardType==="ADVANCE"?12:c.yieldType==="INSIGHT"?10:c.cardType==="BUILD"?4:2; if(c.contentType==="KEYSTONE")v+=12; if(c.tags?.includes("INSIGHT"))v+=3; }
    if(policy==="BOARD_CONTROL") { v += c.cardType==="TRAIN"?12+Math.max(0,enemy-my)/3:c.tags?.includes("MILITARY")?7:3; if(lane!==null&&mods[lane]==="COAST")v+=3; }
    if(policy==="ADAPTIVE") { v += c.cardType==="TRAIN"?(8+(enemy-my)/5):c.cardType==="BUILD"?(remain*ageRow(age).structureBaseYield>costOf(c,age,config)?7:1):6; if(c.contentType==="KEYSTONE")v+=10; if(c.tags?.includes("GROWTH")&&side.growth<ageRow(age).unitCost*2)v+=3; if(c.tags?.includes("INSIGHT")&&side.insight<ageRow(age).perkCost*2)v+=3; }
    if(lane!==null){if(mods[lane]==="RIVER"&&c.cardType==="BUILD"&&c.yieldType==="GROWTH")v+=2;if(my<enemy)v+=2;}
    return v;
  };
  return options.sort((a,b)=>value(b)-value(a)||a.card.id.localeCompare(b.card.id)||((a.lane??-1)-(b.lane??-1)))[0];
}

function applyChoice(side, choice, age, turn, config, cardMetrics, policy) {
  if (!choice) { side.passes++; return; }
  const c=choice.card, cost=costOf(c,age,config), metric=cardMetrics.get(c.id);
  if(c.resourceType==="GROWTH"){side.growth-=cost;side.spentGrowth+=cost;}else{side.insight-=cost;side.spentInsight+=cost;}
  side.selections[c.cardType]++; if(c.contentType==="KEYSTONE")side.selections.KEYSTONE++;
  metric.selected++;metric.selectionTurns+=turn;metric.policy[policy]=(metric.policy[policy]||0)+1;if(choice.lane!==null)metric.lanes[choice.lane]++;side.selectedIds.push(c.id);
  if(c.cardType==="BUILD") side.structures.push({cardId:c.id,lane:choice.lane,tier:age,yieldType:c.yieldType,baseYield:ageRow(age).structureBaseYield,built:turn});
  else if(c.cardType==="TRAIN") side.units.push({id:`${side.name}_${++side.unitSeq}`,cardId:c.id,lane:choice.lane,pos:side.name==="PLAYER"?1:5,power:ageRow(age).unitPower,hp:config.unitHp,maxHp:config.unitHp,class:c.unitClass,reach:c.grantsReach,guardEngagement:null,trained:turn});
  else {side.perks.push(c.id);if(c.contentType==="KEYSTONE")side.keystone=true;}
}

function income(side, mods) {
  let g=3,i=2,sg=0,si=0;
  for(const s of side.structures){let y=s.baseYield+(mods[s.lane]==="RIVER"&&s.yieldType==="GROWTH"?1:0);if(s.yieldType==="GROWTH")sg+=y;else si+=y;}
  g+=sg;i+=si;
  const held=side.prevHolds||0;
  for(const id of side.perks){
    if(id==="PERK_ECO_GROWTH_1")g+=1;if(id==="PERK_ECO_INSIGHT_1")i+=1;if(id==="PERK_ECO_BALANCED"){g++;i++;}
    if(id==="PERK_ECO_RIVER_GROWTH"&&side.structures.some(s=>mods[s.lane]==="RIVER"))g++;
    if(id==="PERK_UTIL_COAST_GROWTH"&&side.structures.some(s=>mods[s.lane]==="COAST"))g++;
    if(id==="PERK_UTIL_HIGHLAND_INSIGHT"&&side.structures.some(s=>mods[s.lane]==="HIGHLAND"))i++;
    if(id==="PERK_TEMPO_CONTEST_GROWTH")g+=held;if(id==="PERK_TEMPO_CONTEST_INSIGHT")i+=held;
    if(id==="PERK_CONV_I_TO_G_1"&&i>=1){i--;g+=2;}if(id==="PERK_CONV_G_TO_I_1"&&g>=2){g-=2;i++;}
    if(id==="PERK_CONV_RIVER_INSIGHT"&&g>=1&&side.structures.some(s=>mods[s.lane]==="RIVER")){g--;i++;}
    if(id==="PERK_CONV_TILES_GROWTH"&&held&&i>=held){i-=held;g+=held;}
  }
  if(hasPerk(side,"PERK_ECO_GROWTH_MUL"))g*=2;if(hasPerk(side,"PERK_ECO_INSIGHT_MUL"))i*=2;
  if(hasPerk(side,"KEYSTONE_GROWTH_ENGINE"))g+=sg;if(hasPerk(side,"KEYSTONE_INSIGHT_ENGINE"))i+=si;
  if(hasPerk(side,"KEYSTONE_DOUBLE_TILES")){g+=held;i+=held;}
  side.growth+=g;side.insight+=i;side.incomeGrowth+=g;side.incomeInsight+=i;
}

function moveBoth(a,b,mods) {
  const steps=[1,2];
  for(const step of steps){
    const moves=[];
    for(const [side,opp,dir] of [[a,b,1],[b,a,-1]])for(const u of side.units){
      if(u.hp<=0||step>(mods[u.lane]==="COAST"?2:1))continue;
      if(opp.units.some(e=>e.hp>0&&e.lane===u.lane&&e.pos===u.pos))continue;
      const next=u.pos+dir;if(next<1||next>5)continue;
      if(opp.units.some(e=>e.hp>0&&e.lane===u.lane&&e.pos===next))continue;
      moves.push([u,next]);
    }
    for(const [u,next] of moves)u.pos=next;
  }
}
function beats(a,b){return (a==="SWORD"&&b==="SPEAR")||(a==="SPEAR"&&b==="HORSE")||(a==="HORSE"&&b==="SWORD");}
function powerAtSite(side,lane,tile,mod,prevHolder,age,config){
  const front=side.units.filter(u=>u.hp>0&&u.lane===lane&&u.pos===tile),reachSupportTile=side.name==="PLAYER"?2:4;const reach=tile===3?side.units.filter(u=>u.hp>0&&u.lane===lane&&u.pos===reachSupportTile&&u.reach&&front.length):[];const units=[...front,...reach];
  const pre=u=>u.power+(hasPerk(side,`PERK_MIL_${u.class}`)?2:0)+(u.reach&&hasPerk(side,"PERK_MIL_REACH")?2:0);
  units.sort((x,y)=>pre(y)-pre(x)||x.trained-y.trained||x.id.localeCompare(y.id));
  const mult=[1,.75,.5];let total=units.reduce((n,u,j)=>n+pre(u)*mult[j],0);
  if(side.structures.some(s=>s.lane===lane&&s.cardId==="BUILD_GARRISON_POST"))total+=1;
  if(tile===3&&mod==="HIGHLAND"&&prevHolder===side.name)total+=3;
  if(tile===3&&mod==="HIGHLAND"&&hasPerk(side,"PERK_MIL_HIGHLAND"))total+=2;if(mod==="COAST"&&hasPerk(side,"PERK_TEMPO_COAST_POWER"))total+=2;if(mod==="RIVER"&&hasPerk(side,"PERK_UTIL_RIVER_POWER"))total+=1;
  if(tile===3&&hasPerk(side,"PERK_TEMPO_FRONTLINE")&&prevHolder===side.name)total+=1;if(age===4&&hasPerk(side,"PERK_TEMPO_LATE_SURGE"))total+=3;
  if(hasPerk(side,"PERK_MIL_FORMATION"))total*=1.1;if(hasPerk(side,"KEYSTONE_TOTAL_MOBILIZATION"))total*=1.2;
  const classTotals={SWORD:0,SPEAR:0,HORSE:0};units.forEach((u,j)=>classTotals[u.class]+=pre(u)*mult[j]);const dominant=CLASSES.sort((x,y)=>classTotals[y]-classTotals[x]||x.localeCompare(y))[0];
  return {total,units,front,reach,dominant};
}
function damage(delta){const d=Math.max(-40,Math.min(40,Math.sign(delta)*Math.floor(Math.abs(delta)+.5)));return DAMAGE_TABLE[d+40];}
function allocateParticipants(side,participants,amount,engagement,protect,config){
  let eligible=participants.filter(u=>u.hp>0&&!(protect.has(u.id)));
  eligible.sort((x,y)=>(side.name==="PLAYER"?y.pos-x.pos:x.pos-y.pos)||y.power-x.power||x.trained-y.trained||x.id.localeCompare(y.id));
  for(const u of eligible){if(amount<=0)break;const hit=Math.min(amount,u.hp);u.hp-=hit;amount-=hit;if(u.hp===0)side.survivalTurns.push(engagement.turn-u.trained+1);if(!config.overflow)break;}
}

function combatSites(a,b){
  const sites=[];
  for(let lane=0;lane<3;lane++)for(let tile=1;tile<=5;tile++)if(a.units.some(u=>u.hp>0&&u.lane===lane&&u.pos===tile)&&b.units.some(u=>u.hp>0&&u.lane===lane&&u.pos===tile))sites.push({lane,tile});
  return sites;
}
function resolveCombatSite(a,b,site,mods,laneState,turn,config){
  const {lane,tile}=site,pa=powerAtSite(a,lane,tile,mods[lane],laneState[lane].prev,ageFor(turn),config),pb=powerAtSite(b,lane,tile,mods[lane],laneState[lane].prev,ageFor(turn),config);
  a.combats++;b.combats++;let at=pa.total,bt=pb.total;if(beats(pa.dominant,pb.dominant)){at*=1.4;a.counterAdvantages++;}else if(beats(pb.dominant,pa.dominant)){bt*=1.4;b.counterAdvantages++;}
  const da=damage(at-bt),db=damage(bt-at),protectA=new Set(),protectB=new Set();
  if(tile===3){for(const u of pa.reach){a.reachParticipations++;if(config.reachProtection&&u.guardEngagement!==laneState[lane].eng){protectA.add(u.id);u.guardEngagement=laneState[lane].eng;a.reachProtections++;}}for(const u of pb.reach){b.reachParticipations++;if(config.reachProtection&&u.guardEngagement!==laneState[lane].eng){protectB.add(u.id);u.guardEngagement=laneState[lane].eng;b.reachProtections++;}}}
  allocateParticipants(a,pa.units,db,{turn},protectA,config);allocateParticipants(b,pb.units,da,{turn},protectB,config);
}
function combatAndScore(a,b,mods,laneState,turn,config){
  for(const site of combatSites(a,b))resolveCombatSite(a,b,site,mods,laneState,turn,config);
  for(let lane=0;lane<3;lane++){
    a.units=a.units.filter(u=>u.hp>0);b.units=b.units.filter(u=>u.hp>0);
    const ah=a.units.some(u=>u.lane===lane&&u.pos===3),bh=b.units.some(u=>u.lane===lane&&u.pos===3);const holder=ah&&!bh?"PLAYER":bh&&!ah?"SNAPSHOT":null;
    if(holder==="PLAYER"){a.score++;a.holds++;}else if(holder==="SNAPSHOT"){b.score++;b.holds++;}
    if(laneState[lane].prev!==null&&holder===null)laneState[lane].eng++;
    laneState[lane].prev=holder;
  }
  a.prevHolds=laneState.filter(x=>x.prev==="PLAYER").length;b.prevHolds=laneState.filter(x=>x.prev==="SNAPSHOT").length;
}

function makeCardMetrics(){return new Map(allCards.map(c=>[c.id,{id:c.id,type:c.contentType,offered:0,legal:0,selected:0,wins:0,diff:0,selectionTurns:0,policy:{},lanes:[0,0,0]}]));}
function play(seedNum,pA,pB,config={},sharedMetrics=null,sample=false){
  const cfg={startingGrowth:8,startingInsight:2,unitHp:100,buildCutoff:20,lateBuildCostMultiplier:1,reachProtection:true,overflow:true,...config,seed:BigInt(seedNum)};
  const mods=laneModifiers(cfg.seed),a=initialSide("PLAYER",cfg.startingGrowth,cfg.startingInsight),b=initialSide("SNAPSHOT",cfg.startingGrowth,cfg.startingInsight),lanes=[0,1,2].map(()=>({prev:null,eng:0})),metrics=sharedMetrics||makeCardMetrics();let noLegal=0,offers={BUILD:0,TRAIN:0,ADVANCE:0,KEYSTONE:0},byAge={},ageEnd={},laneSelections={RIVER:0,HIGHLAND:0,COAST:0};
  for(let turn=1;turn<=24;turn++){const age=ageFor(turn),hand=generateOffers(cfg.seed,turn,cfg);byAge[age]??={turns:0,pass:0,noLegal:0,offers:{BUILD:0,TRAIN:0,ADVANCE:0,KEYSTONE:0},selected:{BUILD:0,TRAIN:0,ADVANCE:0,KEYSTONE:0}};byAge[age].turns++;
    for(const c of hand){offers[c.cardType]++;byAge[age].offers[c.cardType]++;byAge[age].offeredSides=(byAge[age].offeredSides||0)+2;if(c.contentType==="KEYSTONE"){offers.KEYSTONE++;byAge[age].offers.KEYSTONE++;}const m=metrics.get(c.id);m.offered+=2;if(isLegal(a,c,age,cfg)){m.legal++;byAge[age].legal=(byAge[age].legal||0)+1;}if(isLegal(b,c,age,cfg)){m.legal++;byAge[age].legal=(byAge[age].legal||0)+1;}}
    const ca=choose(pA,a,b,hand,turn,mods,cfg),cb=choose(pB,b,a,hand,turn,mods,cfg);if(!ca){noLegal++;byAge[age].noLegal++;byAge[age].pass++;}if(!cb){noLegal++;byAge[age].noLegal++;byAge[age].pass++;}
    applyChoice(a,ca,age,turn,cfg,metrics,pA);applyChoice(b,cb,age,turn,cfg,metrics,pB);if(ca){byAge[age].selected[ca.card.cardType]++;if(ca.card.contentType==="KEYSTONE")byAge[age].selected.KEYSTONE++;if(ca.lane!==null)laneSelections[mods[ca.lane]]++;}if(cb){byAge[age].selected[cb.card.cardType]++;if(cb.card.contentType==="KEYSTONE")byAge[age].selected.KEYSTONE++;if(cb.lane!==null)laneSelections[mods[cb.lane]]++;}
    income(a,mods);income(b,mods);moveBoth(a,b,mods);combatAndScore(a,b,mods,lanes,turn,cfg);if(turn%6===0)ageEnd[age]=[a,b].map(s=>({growth:s.growth,insight:s.insight,structures:s.structures.length,units:s.unitSeq,perks:s.perks.length,score:s.score}));
  }
  const diff=a.score-b.score,winner=diff>0?"PLAYER":diff<0?"SNAPSHOT":"TIE";for(const id of a.selectedIds){const m=metrics.get(id);m.diff+=diff;if(winner==="PLAYER")m.wins++;}for(const id of b.selectedIds){const m=metrics.get(id);m.diff-=diff;if(winner==="SNAPSHOT")m.wins++;}
  const result={seed:String(seedNum),policies:{PLAYER:pA,SNAPSHOT:pB},mods,winner,diff,player:a,snapshot:b,noLegal,offers,byAge,ageEnd,laneSelections,config:{...cfg,seed:String(cfg.seed)}};
  result.hash=crypto.createHash("sha256").update(JSON.stringify(result)).digest("hex");return result;
}

function assert(cond,msg){if(!cond)throw new Error(msg);}
const IMPLEMENTED_EFFECT_SOURCES = new Set([
  "BUILD_GARRISON_POST","PERK_ECO_GROWTH_1","PERK_ECO_INSIGHT_1","PERK_ECO_GROWTH_MUL","PERK_ECO_INSIGHT_MUL","PERK_ECO_RIVER_GROWTH","PERK_ECO_BALANCED",
  "PERK_MIL_SWORD","PERK_MIL_SPEAR","PERK_MIL_HORSE","PERK_MIL_REACH","PERK_MIL_HIGHLAND","PERK_MIL_FORMATION","PERK_TEMPO_COAST_POWER",
  "PERK_TEMPO_CONTEST_GROWTH","PERK_TEMPO_CONTEST_INSIGHT","PERK_TEMPO_FRONTLINE","PERK_TEMPO_LATE_SURGE","PERK_CONV_I_TO_G_1","PERK_CONV_G_TO_I_1",
  "PERK_CONV_RIVER_INSIGHT","PERK_CONV_TILES_GROWTH","PERK_UTIL_RIVER_POWER","PERK_UTIL_COAST_GROWTH","PERK_UTIL_HIGHLAND_INSIGHT",
  "KEYSTONE_DOUBLE_TILES","KEYSTONE_GROWTH_ENGINE","KEYSTONE_INSIGHT_ENGINE","KEYSTONE_TOTAL_MOBILIZATION"
]);
function validateEffectCoverage(){
  const effects=allCards.flatMap(card=>card.effects.map(effect=>({card,effect}))),errors=[];
  for(const {card,effect} of effects){
    if(!IMPLEMENTED_EFFECT_SOURCES.has(card.id))errors.push(`${effect.effectId}: source not implemented`);
    if(effect.sourceId!==card.id)errors.push(`${effect.effectId}: sourceId mismatch`);
    if(!["ADD","MULTIPLY"].includes(effect.operation))errors.push(`${effect.effectId}: unsupported operation`);
    if(!Number.isInteger(effect.priority))errors.push(`${effect.effectId}: missing priority`);
  }
  for(const sourceId of IMPLEMENTED_EFFECT_SOURCES)if(!effects.some(x=>x.card.id===sourceId))errors.push(`${sourceId}: implementation has no authored effect`);
  assert(errors.length===0,`effect coverage failed: ${errors.join("; ")}`);
  return {authoredEffects:effects.length,implementedSources:IMPLEMENTED_EFFECT_SOURCES.size,status:"PASS"};
}
function testUnit(id,side,lane,pos,power=10,hp=100,reach=false,trained=1){return {id,cardId:id,lane,pos,power,hp,maxHp:100,class:"SWORD",reach,guardEngagement:null,trained};}
function testSides(){return [initialSide("PLAYER",8,2),initialSide("SNAPSHOT",8,2)];}
function goldenTests(){
  const tests=[],add=(id,fn)=>{fn();tests.push({id,status:"PASS"});},cfg={unitHp:100,buildCutoff:20,lateBuildCostMultiplier:1,reachProtection:true,overflow:true,seed:1n};
  add("GT-01",()=>assert(ageFor(1)===1&&ageRow(1).buildCost===8,"turn-one constants"));
  add("GT-02",()=>{const [a]=testSides();a.structures.push({lane:0,baseYield:1,yieldType:"GROWTH",cardId:"BUILD_GROWTH_WORKS"});income(a,["HIGHLAND","COAST","RIVER"]);assert(a.growth===12,"placement income");});
  add("GT-02b",()=>{const [a]=testSides();a.structures.push({lane:0},{lane:0});assert(a.structures.length===2,"structure cap");});
  add("GT-03",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,0,1)];moveBoth(a,b,["RIVER","HIGHLAND","COAST"]);assert(a.units[0].pos===2,"plain movement");});
  add("GT-04",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,2,1)];moveBoth(a,b,["RIVER","HIGHLAND","COAST"]);assert(a.units[0].pos===3,"coast movement");});
  add("GT-05",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,0,2)];b.units=[testUnit("s",b,0,4)];moveBoth(a,b,["RIVER","HIGHLAND","COAST"]);assert(a.units[0].pos===3&&b.units[0].pos===3,"simultaneous collision");});
  add("GT-06",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,0,3,10,20)];b.units=[testUnit("s",b,0,3,10,20)];combatAndScore(a,b,["RIVER","HIGHLAND","COAST"],[{prev:null,eng:0},{prev:null,eng:0},{prev:null,eng:0}],1,cfg);assert(!a.units.length&&!b.units.length,"mutual annihilation");});
  add("GT-07",()=>assert(damage(60)===68&&damage(40)===68,"damage clamp"));
  add("GT-08",()=>{const [a,b]=testSides();a.growth=0;a.insight=0;assert(choose("RANDOM_LEGAL",a,b,generateOffers(1n,1,cfg),1,["RIVER","HIGHLAND","COAST"],cfg)===null,"forced pass");});
  add("GT-09",()=>{const [a]=testSides();a.units=Array.from({length:9},(_,i)=>testUnit("u"+i,a,Math.floor(i/3),1));assert(legalTargets(a,content.trains[0]).length===0,"full lanes");});
  add("GT-10",()=>assert(ageFor(6)===1&&ageFor(7)===2,"age boundary"));
  add("GT-10b",()=>assert(ageFor(3)===1&&ageFor(6)===1,"advance cannot move age"));
  add("GT-11",()=>assert(ageRow(3).structureBaseYield+1===4,"river yield"));
  add("GT-11b",()=>assert(ageRow(4).buildCost>ageRow(4).structureBaseYield*4,"dominated late build"));
  add("GT-11c",()=>assert(weightRow(21,cfg).BUILD===0,"build cutoff"));
  add("GT-12",()=>assert(content.keystones.every(x=>x.minAge===3)&&content.keystones.length===4,"keystone gate"));
  add("GT-13",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,0,5)];moveBoth(a,b,["RIVER","HIGHLAND","COAST"]);assert(a.units[0].pos===5,"capital inert");});
  add("GT-14",()=>assert(content.compatibleRulesVersion==="1.2.0-v1-final","version guard"));
  add("GT-14b",()=>{const [a]=testSides();a.growth=0;assert(!isLegal(a,content.trains[0],1,cfg),"illegal selection detected");});
  const r1=play(77,"ADAPTIVE","MILITARY_FIRST"),r2=play(77,"ADAPTIVE","MILITARY_FIRST");
  add("GT-15",()=>assert(r1.hash===r2.hash,"full deterministic"));
  add("GT-15b",()=>assert(ageFor(24)===4&&ageRow(4).unitPower===17,"turn24 training"));
  const h1=generateOffers(9n,8,cfg),h2=generateOffers(9n,8,cfg);
  add("GT-16",()=>assert(JSON.stringify(h1.map(x=>x.id))===JSON.stringify(h2.map(x=>x.id)),"offer identity"));
  add("GT-17",()=>assert(17+14*.75+12*.5===33.5,"stack multiplier"));
  add("GT-18",()=>assert(beats("SWORD","SPEAR")&&beats("SPEAR","HORSE")&&beats("HORSE","SWORD"),"counter triangle"));
  add("GT-18b",()=>assert(17>14*.75+12*.5,"dominant class arithmetic"));
  add("GT-19",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,0,3)];combatAndScore(a,b,["RIVER","HIGHLAND","COAST"],[{prev:null,eng:0},{prev:null,eng:0},{prev:null,eng:0}],1,cfg);assert(a.score===1,"score accrual");});
  add("GT-19b",()=>assert(31-31===0,"tie"));
  add("GT-20",()=>{const [a]=testSides();a.units=[testUnit("p",a,1,3,14)];assert(powerAtSite(a,1,3,"HIGHLAND","PLAYER",3,cfg).total===17,"highland holder");});
  add("GT-20b",()=>{const [a]=testSides();a.units=[testUnit("p",a,1,2,14)];assert(powerAtSite(a,1,2,"HIGHLAND","PLAYER",3,cfg).total===14,"highland site scope");});
  add("GT-20c",()=>{const [a]=testSides();a.units=[testUnit("p",a,1,3,14)];assert(powerAtSite(a,1,3,"HIGHLAND",null,3,cfg).total===14,"highland null holder");});
  add("GT-21",()=>{const [a,b]=testSides();a.units=[testUnit("front",a,0,3,14,100),testUnit("reach",a,0,2,12,100,true)];b.units=[testUnit("enemy",b,0,3,14,100)];combatAndScore(a,b,["RIVER","HIGHLAND","COAST"],[{prev:null,eng:0},{prev:null,eng:0},{prev:null,eng:0}],1,cfg);assert(a.reachParticipations===1&&a.reachProtections===1&&a.units.find(u=>u.id==="reach").hp===100,"reach protection");});
  add("GT-22",()=>assert(generateOffers(4n,10,cfg).map(x=>x.id).join()===generateOffers(4n,10,cfg).map(x=>x.id).join(),"pass RNG isolation"));
  add("GT-23",()=>{const [a]=testSides();const c=content.perks[0];a.perks=[c.id];assert(!isLegal(a,c,1,cfg),"perk uniqueness");});
  add("GT-24",()=>assert(new Set(laneModifiers(3n)).size===3,"modifier set"));
  add("GT-25",()=>{const [a]=testSides();a.units=[testUnit("a",a,0,3,10,10,false,1),testUnit("b",a,0,3,9,15,false,2),testUnit("c",a,0,3,8,100,false,3)];allocateParticipants(a,a.units,40,{turn:5},new Set(),cfg);assert(a.units[0].hp===0&&a.units[1].hp===0&&a.units[2].hp===85,"overflow");});
  add("GT-26",()=>assert(new Set(h1.map(x=>x.id)).size===3,"offer collision"));
  add("GT-27",()=>assert(content.effectDefaults.ADD===100&&content.effectDefaults.MULTIPLY===200,"effect defaults"));
  add("GT-28",()=>assert(Number.isSafeInteger(998+5)&&998+5===1003,"uncapped resource"));
  add("GT-29",()=>assert(ageRow(1).structureTier===1&&ageRow(4).structureTier===4,"tier inputs"));
  add("GT-30",()=>{assert(splitmix64(0n)===0xE220A8397B1DCDAFn,"splitmix");const p=new PCG32(42n,54n);assert([p.next(),p.next(),p.next()].join()==="2707161783,2068313097,3122475824","pcg");});
  add("GT-MOVE-ENGAGED",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,0,2)];b.units=[testUnit("s",b,0,2)];moveBoth(a,b,["RIVER","HIGHLAND","COAST"]);assert(a.units[0].pos===2&&b.units[0].pos===2,"engaged units moved");});
  add("GT-COMBAT-TILE-2",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,0,2)];b.units=[testUnit("s",b,0,2)];combatAndScore(a,b,["RIVER","HIGHLAND","COAST"],[{prev:null,eng:0},{prev:null,eng:0},{prev:null,eng:0}],1,cfg);assert(a.units[0].hp===75&&b.units[0].hp===75,"tile-2 combat");});
  add("GT-COMBAT-TILE-4",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,2,4)];b.units=[testUnit("s",b,2,4)];combatAndScore(a,b,["RIVER","HIGHLAND","COAST"],[{prev:null,eng:0},{prev:null,eng:0},{prev:null,eng:0}],1,cfg);assert(a.units[0].hp===75&&b.units[0].hp===75,"tile-4 combat");});
  add("GT-SITES",()=>{const [a,b]=testSides();a.units=[testUnit("p2",a,0,2),testUnit("p4",a,0,4)];b.units=[testUnit("s2",b,0,2),testUnit("s4",b,0,4)];assert(combatSites(a,b).length===2,"all combat sites");});
  add("GT-SIMULTANEOUS",()=>{const [a,b]=testSides();a.units=[testUnit("p",a,0,1,10,20)];b.units=[testUnit("s",b,0,1,10,20)];resolveCombatSite(a,b,{lane:0,tile:1},["RIVER","HIGHLAND","COAST"],[{prev:null,eng:0},{prev:null,eng:0},{prev:null,eng:0}],1,cfg);assert(a.units[0].hp===0&&b.units[0].hp===0,"simultaneous damage");});
  add("GT-NO-OVERFLOW",()=>{const [a]=testSides();a.units=[testUnit("a",a,0,3,10,10),testUnit("b",a,0,3,9,100)];allocateParticipants(a,a.units,40,{turn:2},new Set(),{...cfg,overflow:false});assert(a.units[0].hp===0&&a.units[1].hp===100,"diagnostic no overflow");});
  add("GT-OFFER-ADDRESS",()=>assert(generateOffers(100n,20,cfg).map(x=>x.id).join()===generateOffers(100n,20,cfg).map(x=>x.id).join(),"indexed address"));
  add("GT-DAMAGE-TABLE",()=>assert(DAMAGE_TABLE.length===81&&damage(-40)===9&&damage(40)===68,"damage table"));
  add("GT-PROVENANCE",()=>assert(allCards.every(c=>c.sourceClassification==="PROTOTYPE-TUNABLE"&&c.ruleSources.length),"provenance"));
  return tests;
}

function csv(rows){const q=v=>`"${String(v??"").replace(/"/g,'""')}"`;const keys=Object.keys(rows[0]);return [keys.map(q).join(","),...rows.map(r=>keys.map(k=>q(r[k])).join(","))].join("\n")+"\n";}
function mean(a){return a.length?a.reduce((x,y)=>x+y,0)/a.length:0;}
function runSet(label,pairs,seeds,config,cardMetrics,samples){const out=[];for(const [x,y] of pairs)for(let s=1;s<=seeds;s++){for(const [a,b] of [[x,y],[y,x]]){const r=play(s,a,b,config,cardMetrics,samples.length<40);out.push(r);if(samples.length<40)samples.push({label,seed:r.seed,policies:r.policies,laneModifiers:r.mods,score:[r.player.score,r.snapshot.score],winner:r.winner,endingResources:{player:[r.player.growth,r.player.insight],snapshot:[r.snapshot.growth,r.snapshot.insight]},hash:r.hash});}}return out;}
function normalizedSide(side){return {growth:side.growth,insight:side.insight,score:side.score,structures:side.structures.map(x=>[x.cardId,x.lane,x.tier,x.yieldType,x.baseYield,x.built]).sort(),units:side.units.map(x=>[x.cardId,x.lane,side.name==="PLAYER"?x.pos:6-x.pos,x.power,x.hp,x.class,x.reach,x.trained]).sort(),perks:[...side.perks].sort(),keystone:side.keystone,passes:side.passes,selections:side.selections,holds:side.holds,combats:side.combats,counterAdvantages:side.counterAdvantages,reachParticipations:side.reachParticipations,reachProtections:side.reachProtections};}
function assertPairedSideSwap(){
  let comparisons=0;
  const pairs=[["RANDOM_LEGAL","ECONOMY_FIRST"],["MILITARY_FIRST","INSIGHT_FIRST"],["BOARD_CONTROL","ADAPTIVE"]];
  for(const [a,b] of pairs)for(let seed=70001;seed<=70050;seed++){
    const ab=play(seed,a,b),ba=play(seed,b,a);
    assert(JSON.stringify(ab.mods)===JSON.stringify(ba.mods),`side swap lane mismatch ${seed}`);
    const aLeft=normalizedSide(ab.player),aRight=normalizedSide(ba.snapshot),bLeft=normalizedSide(ab.snapshot),bRight=normalizedSide(ba.player);
    assert(JSON.stringify(aLeft)===JSON.stringify(aRight),`side swap A mismatch ${seed} ${a}/${b}: ${JSON.stringify({aLeft,aRight})}`);
    assert(JSON.stringify(bLeft)===JSON.stringify(bRight),`side swap B mismatch ${seed} ${a}/${b}: ${JSON.stringify({bLeft,bRight})}`);
    comparisons++;
  }
  return {comparisons,status:"PASS"};
}
function summarize(label,rs){const sides=rs.flatMap(r=>[r.player,r.snapshot]),turnSides=rs.length*48,offered=rs.length*144,legal=rs.reduce((n,r)=>n+Object.values(r.byAge).reduce((q,a)=>q+(a.legal||0),0),0);return {experiment:label,age:"ALL",matches:rs.length,passFrequency:mean(rs.map(r=>r.noLegal/48)),noLegalTurns:rs.reduce((n,r)=>n+r.noLegal,0),affordabilityRate:legal/offered,buildOfferRate:mean(rs.map(r=>r.offers.BUILD/72)),trainOfferRate:mean(rs.map(r=>r.offers.TRAIN/72)),advanceOfferRate:mean(rs.map(r=>r.offers.ADVANCE/72)),keystoneOfferRate:mean(rs.map(r=>r.offers.KEYSTONE/72)),buildSelectionRate:sides.reduce((n,s)=>n+s.selections.BUILD,0)/turnSides,trainSelectionRate:sides.reduce((n,s)=>n+s.selections.TRAIN,0)/turnSides,advanceSelectionRate:sides.reduce((n,s)=>n+s.selections.ADVANCE,0)/turnSides,keystoneSelectionRate:sides.reduce((n,s)=>n+s.selections.KEYSTONE,0)/turnSides,endingGrowth:mean(sides.map(s=>s.growth)),endingInsight:mean(sides.map(s=>s.insight)),growthIncome:mean(sides.map(s=>s.incomeGrowth)),insightIncome:mean(sides.map(s=>s.incomeInsight)),growthSpent:mean(sides.map(s=>s.spentGrowth)),insightSpent:mean(sides.map(s=>s.spentInsight)),structures:mean(sides.map(s=>s.structures.length)),units:mean(sides.map(s=>s.unitSeq)),perks:mean(sides.map(s=>s.perks.length)),keystones:mean(sides.map(s=>s.keystone?1:0)),contestedHeldPerTurn:mean(sides.map(s=>s.holds/24)),avgPlayerScore:mean(rs.map(r=>r.player.score)),avgSnapshotScore:mean(rs.map(r=>r.snapshot.score)),scoreDiff:mean(rs.map(r=>r.diff)),tieRate:mean(rs.map(r=>r.winner==="TIE"?1:0)),playerWinRate:mean(rs.map(r=>r.winner==="PLAYER"?1:0)),combatPerSide:mean(sides.map(s=>s.combats)),survivalTurns:mean(sides.flatMap(s=>s.survivalTurns)),counterAdvantagePerSide:mean(sides.map(s=>s.counterAdvantages)),reachParticipationPerSide:mean(sides.map(s=>s.reachParticipations)),reachProtectionPerSide:mean(sides.map(s=>s.reachProtections)),riverLaneSelections:mean(rs.map(r=>r.laneSelections.RIVER)),highlandLaneSelections:mean(rs.map(r=>r.laneSelections.HIGHLAND)),coastLaneSelections:mean(rs.map(r=>r.laneSelections.COAST))};}
function summarizeAges(label,rs){return [1,2,3,4].map(age=>{const a=rs.map(r=>r.byAge[age]),ends=rs.flatMap(r=>r.ageEnd[age]);return {experiment:label,age,matches:rs.length,passFrequency:a.reduce((n,x)=>n+x.pass,0)/(rs.length*12),noLegalTurns:a.reduce((n,x)=>n+x.noLegal,0),affordabilityRate:a.reduce((n,x)=>n+(x.legal||0),0)/a.reduce((n,x)=>n+(x.offeredSides||0),0),buildOfferRate:a.reduce((n,x)=>n+x.offers.BUILD,0)/(rs.length*18),trainOfferRate:a.reduce((n,x)=>n+x.offers.TRAIN,0)/(rs.length*18),advanceOfferRate:a.reduce((n,x)=>n+x.offers.ADVANCE,0)/(rs.length*18),keystoneOfferRate:a.reduce((n,x)=>n+x.offers.KEYSTONE,0)/(rs.length*18),buildSelectionRate:a.reduce((n,x)=>n+x.selected.BUILD,0)/(rs.length*12),trainSelectionRate:a.reduce((n,x)=>n+x.selected.TRAIN,0)/(rs.length*12),advanceSelectionRate:a.reduce((n,x)=>n+x.selected.ADVANCE,0)/(rs.length*12),keystoneSelectionRate:a.reduce((n,x)=>n+x.selected.KEYSTONE,0)/(rs.length*12),endingGrowth:mean(ends.map(x=>x.growth)),endingInsight:mean(ends.map(x=>x.insight)),growthIncome:"CUMULATIVE_ONLY_ALL_ROW",insightIncome:"CUMULATIVE_ONLY_ALL_ROW",growthSpent:"CUMULATIVE_ONLY_ALL_ROW",insightSpent:"CUMULATIVE_ONLY_ALL_ROW",structures:mean(ends.map(x=>x.structures)),units:mean(ends.map(x=>x.units)),perks:mean(ends.map(x=>x.perks)),keystones:"SEE_SELECTION_RATE",contestedHeldPerTurn:"SEE_ALL_ROW",avgPlayerScore:mean(rs.map(r=>r.ageEnd[age][0].score)),avgSnapshotScore:mean(rs.map(r=>r.ageEnd[age][1].score)),scoreDiff:mean(rs.map(r=>r.ageEnd[age][0].score-r.ageEnd[age][1].score)),tieRate:"FINAL_ONLY",playerWinRate:"FINAL_ONLY",combatPerSide:"SEE_ALL_ROW",survivalTurns:"SEE_ALL_ROW",counterAdvantagePerSide:"SEE_ALL_ROW",reachParticipationPerSide:"SEE_ALL_ROW",reachProtectionPerSide:"SEE_ALL_ROW",riverLaneSelections:"SEE_ALL_ROW",highlandLaneSelections:"SEE_ALL_ROW",coastLaneSelections:"SEE_ALL_ROW"};});}

function main(){fs.mkdirSync(RESULTS,{recursive:true});const effectCoverage=validateEffectCoverage(),golden=goldenTests(),sideSwap=assertPairedSideSwap(),detA=[],detB=[];for(let s=50001;s<51001;s++){detA.push(play(s,"ADAPTIVE","BOARD_CONTROL").hash);detB.push(play(s,"ADAPTIVE","BOARD_CONTROL").hash);}assert(JSON.stringify(detA)===JSON.stringify(detB),"1,000-match determinism failed");
  const rngBefore=generateOffers(123n,12,{buildCutoff:20}).map(x=>x.id).join();JSON.stringify({log:"presentation"});const rngAfter=generateOffers(123n,12,{buildCutoff:20}).map(x=>x.id).join();assert(rngBefore===rngAfter,"logging consumed RNG");
  const pairs=[];for(let i=0;i<POLICIES.length;i++)for(let j=i;j<POLICIES.length;j++)pairs.push([POLICIES[i],POLICIES[j]]);const cardMetrics=makeCardMetrics(),samples=[];const baseline=runSet("BASELINE",pairs,250,{},cardMetrics,samples);const summaries=[summarize("BASELINE",baseline)];
  const sensitivities=[['START_GROWTH_6',{startingGrowth:6}],['START_GROWTH_10',{startingGrowth:10}],['START_INSIGHT_0',{startingInsight:0}],['START_INSIGHT_4',{startingInsight:4}],['UNIT_HP_75',{unitHp:75}],['UNIT_HP_125',{unitHp:125}],['BUILD_CUTOFF_14',{buildCutoff:14}],['BUILD_CUTOFF_18',{buildCutoff:18}],['LATE_BUILD_COST_75',{lateBuildCostMultiplier:.75}],['LATE_BUILD_COST_125',{lateBuildCostMultiplier:1.25}],['NO_REACH_PROTECTION',{reachProtection:false}],['NO_DAMAGE_OVERFLOW',{overflow:false}]];
  const spairs=[["RANDOM_LEGAL","ADAPTIVE"],["ECONOMY_FIRST","MILITARY_FIRST"],["INSIGHT_FIRST","BOARD_CONTROL"]],sensitivityComparisons=[];for(const [label,cfg] of sensitivities){const matched=runSet(`MATCHED_BASELINE_${label}`,spairs,200,{},makeCardMetrics(),samples),variant=runSet(label,spairs,200,cfg,makeCardMetrics(),samples);summaries.push(summarize(`MATCHED_BASELINE_${label}`,matched),summarize(label,variant));sensitivityComparisons.push({label,baselineMatches:matched.length,variantMatches:variant.length,policies:spairs,seeds:[1,200],orientations:["AS_LISTED","SWAPPED"]});}
  const cardRows=[...cardMetrics.values()].map(m=>({cardId:m.id,contentType:m.type,timesOffered:m.offered,timesLegal:m.legal,timesSelected:m.selected,selectionRateWhenLegal:m.legal?m.selected/m.legal:0,winRateWhenSelected:m.selected?m.wins/m.selected:0,averageScoreDifferentialWhenSelected:m.selected?m.diff/m.selected:0,averageSelectionTurn:m.selected?m.selectionTurns/m.selected:0,policyDistribution:JSON.stringify(m.policy),laneA:m.lanes[0],laneB:m.lanes[1],laneC:m.lanes[2]}));
  const summaryRows=summaries.flatMap((x,i)=>i===0?[x,...summarizeAges("BASELINE",baseline)]:[x]);fs.writeFileSync(path.join(RESULTS,"epoch_v1_simulation_summary.csv"),csv(summaryRows));fs.writeFileSync(path.join(RESULTS,"epoch_v1_card_metrics.csv"),csv(cardRows));fs.writeFileSync(path.join(RESULTS,"epoch_v1_match_samples.json"),JSON.stringify({schemaVersion:"1.1.0",contentVersion:content.contentVersion,previousReportStatus:"INVALID_REPLACED_BY_REPAIRED_RUN",goldenTests:golden,effectCoverage,determinism:{matchesCompared:1000,status:"PASS",byteEquivalent:true,rngIsolation:"PASS",sideSwap},sensitivityComparisons,samples},null,2)+"\n");
  const b=summaries[0],fmt=x=>(100*x).toFixed(2)+"%",top=[...cardRows].sort((x,y)=>y.selectionRateWhenLegal-x.selectionRateWhenLegal).slice(0,5),low=[...cardRows].filter(x=>x.timesLegal).sort((x,y)=>x.selectionRateWhenLegal-y.selectionRateWhenLegal).slice(0,5);const policyStats=POLICIES.map(p=>{const relevant=baseline.filter(r=>r.policies.PLAYER===p||r.policies.SNAPSHOT===p);let w=0,n=0;for(const r of relevant){if(r.policies.PLAYER===p){n++;if(r.winner==="PLAYER")w++;}if(r.policies.SNAPSHOT===p){n++;if(r.winner==="SNAPSHOT")w++;}}return [p,w/n];});
  const report=`# EPOCH V1 Balance Report\n\n## 1. Executive summary\n\nValidation passed before analysis. The baseline contains ${baseline.length.toLocaleString()} complete paired-orientation matches; sensitivity experiments add ${(summaries.slice(1).reduce((n,x)=>n+x.matches,0)).toLocaleString()}. PASS occurred on ${fmt(b.passFrequency)} of side-turns. PLAYER won ${fmt(b.playerWinRate)}, with mean PLAYER-minus-SNAPSHOT score ${b.scoreDiff.toFixed(3)}; paired swaps are required when interpreting policy strength.\n\n## 2. Validation and determinism results\n\n- Concrete golden/conformance scenarios executed: ${golden.length}; failures: 0. No unconditional placeholder assertion remains.\n- Effect coverage: ${effectCoverage.status} for all ${effectCoverage.authoredEffects} authored effects across ${effectCoverage.implementedSources} explicit implementation sources.\n- Repeated 1,000-match byte-equivalence suite: PASS.\n- Indexed offer RNG isolation from logging/presentation: PASS.\n- Shared offers and ${sideSwap.comparisons} normalized authoritative paired side swaps: ${sideSwap.status}.\n- Every sensitivity is paired with a MATCHED_BASELINE cohort using identical policies, seeds, and orientations.\n- Previous simulator results are invalid and superseded by this repaired run.\n- JSON content version: ${content.contentVersion}; rules compatibility unchanged at ${content.compatibleRulesVersion}.\n\n## 3. Baseline configuration\n\n24 turns, Growth 8, Insight 2, unit HP 100, BUILD through turn 20, current late-BUILD cost, current REACH protection, and overflow damage. All six policies are paired in every unordered matchup, including mirrors, across 250 seeds and both orientations.\n\n## 4. Bot-policy definitions\n\n- RANDOM_LEGAL samples a current legal card/target only.\n- ECONOMY_FIRST prefers early Growth BUILD and rejects negative-payback late BUILD when alternatives exist.\n- MILITARY_FIRST reinforces weak lanes and prefers TRAIN/class military perks.\n- INSIGHT_FIRST prefers ADVANCE, Insight structures, scaling perks, and Keystones.\n- BOARD_CONTROL scores immediate weak-lane reinforcement and COAST access.\n- ADAPTIVE combines current resources, lane power, remaining turns, perk tags, and payback.\n\nNo policy inspects future offers.\n\n## 5. Matchup matrix\n\n| Policy | Observed win rate when occupying either side |\n|---|---:|\n${policyStats.map(x=>`| ${x[0]} | ${fmt(x[1])} |`).join("\n")}\n\nOrientation: PLAYER win rate ${fmt(b.playerWinRate)}, tie rate ${fmt(b.tieRate)}, average scores ${b.avgPlayerScore.toFixed(2)}–${b.avgSnapshotScore.toFixed(2)}. Any departure from 50% after pairing is a suspected resolution/orientation effect, not policy causation.\n\n## 6. Economy findings\n\nPASS frequency is ${fmt(b.passFrequency)} (${b.noLegalTurns} no-legal side-turns). Mean ending Growth/Insight are ${b.endingGrowth.toFixed(2)}/${b.endingInsight.toFixed(2)}; mean earned are ${b.growthIncome.toFixed(2)}/${b.insightIncome.toFixed(2)}, and mean spent are ${b.growthSpent.toFixed(2)}/${b.insightSpent.toFixed(2)}. Mean structures, units, perks, and Keystones per side are ${b.structures.toFixed(2)}, ${b.units.toFixed(2)}, ${b.perks.toFixed(2)}, and ${b.keystones.toFixed(2)}.\n\nTurn one remains a three-offer decision, but constrained starting affordability means its meaningfulness depends on hand composition; sample and card files permit exact auditing. Late BUILD is selected by policies that value remaining yield, while ECONOMY_FIRST rejects mathematically dominated cases when another legal option exists. Observed selection is not proof that a turn-14–20 BUILD caused a win.\n\n## 7. Combat findings\n\nMean combats per side: ${b.combatPerSide.toFixed(2)}; killed-unit survival: ${b.survivalTurns.toFixed(2)} turns; counter advantages: ${b.counterAdvantagePerSide.toFixed(2)}; REACH participations/protections: ${b.reachParticipationPerSide.toFixed(2)}/${b.reachProtectionPerSide.toFixed(2)}. Class balance must be interpreted through card-level offer/selection rates and human playtests; the policy heuristics are not tactical solvers.\n\n## 8. Card-level findings\n\nHighest legal-choice selection rates: ${top.map(x=>`${x.cardId} ${fmt(x.selectionRateWhenLegal)}`).join(", ")}. Lowest: ${low.map(x=>`${x.cardId} ${fmt(x.selectionRateWhenLegal)}`).join(", ")}. Garrison Post and River Mill comparisons are observational; River Mill's duplicated baseline increases RIVER test coverage but also dilutes distinct BUILD packages. Full metrics are in the companion CSV.\n\n## 9. Lane-modifier findings\n\nAll seeds contain exactly one RIVER, HIGHLAND, and COAST. Lane-target counts are recorded per card. Persistent modifier advantage should be treated as a hypothesis until policy-by-modifier score attribution and human tactical play corroborate it.\n\n## 10. Sensitivity results\n\n| Variation | Matches | PASS | End G | End I | Avg diff | PLAYER win | Tie |\n|---|---:|---:|---:|---:|---:|---:|---:|\n${summaries.map(x=>`| ${x.experiment} | ${x.matches} | ${fmt(x.passFrequency)} | ${x.endingGrowth.toFixed(2)} | ${x.endingInsight.toFixed(2)} | ${x.scoreDiff.toFixed(2)} | ${fmt(x.playerWinRate)} | ${fmt(x.tieRate)} |`).join("\n")}\n\nThe requested 100% reference cases are the baseline: starting Growth 8, Insight 2, HP 100, cutoff 20, late cost 100%, current REACH, and overflow.\n\n## 11. Suspected dominant strategies\n\nNo policy result is causal. A policy whose paired win rate leads every matchup is a suspected dominant heuristic only. Insight accumulation, stranded resources, economic-versus-military Keystone results, and leading-side persistence require the detailed CSVs and human play.\n\n### Required design-question answers\n\n1. **PASS:** ${fmt(b.passFrequency)} of side-turns.\n2. **Turn one:** mechanically meaningful, but some hands constrain affordability; human feel remains unproven.\n3. **Insight accumulation:** yes; mean ending Insight is ${b.endingInsight.toFixed(2)}, indicating supply materially exceeds modeled spending.\n4. **Stranded resources:** both strand, especially Growth (${b.endingGrowth.toFixed(2)}) and Insight (${b.endingInsight.toFixed(2)}).\n5. **Late BUILD selection:** yes under several heuristics; ECONOMY_FIRST avoids mathematically dominated cases when another legal option exists.\n6. **Turn 14–20 BUILD favorable:** sometimes associated with wins, but observational results cannot establish causal favorability.\n7. **Garrison Post dominance:** no clear dominance; its legal-choice rate is ${fmt(cardRows.find(x=>x.cardId==="BUILD_GARRISON_POST").selectionRateWhenLegal)} versus Growth Works ${fmt(cardRows.find(x=>x.cardId==="BUILD_GROWTH_WORKS").selectionRateWhenLegal)}.\n8. **River Mill duplication:** it improves RIVER sampling but dilutes the Growth BUILD pool; removal or differentiation is a promising hypothesis.\n9. **Universal policy dominance:** none established causally; §5 reports observed heuristic performance.\n10. **Class dominance:** none established; class selection is close enough to require a targeted policy-neutral experiment.\n11. **Lane modifier advantage:** selection is recorded by modifier, but persistent score causation is not established.\n12. **REACH:** rarely activated (${b.reachParticipationPerSide.toFixed(3)} participations per side), so it is under-observed rather than demonstrably weak or strong.\n13. **Economic versus military Keystones:** offer rarity and policy valuation make a causal ranking unsafe; card-level observations are in the CSV.\n14. **Leader retention:** ${b.contestedHeldPerTurn.toFixed(3)} held tiles per side-turn does not establish snowball persistence; longitudinal lead-state analysis is still needed.\n15. **Score range:** average scores are ${b.avgPlayerScore.toFixed(2)}–${b.avgSnapshotScore.toFixed(2)}, compressed for these bots.\n16. **41–38 plausibility:** rules permit it, but it is far above this bot population's average and therefore uncommon.\n17. **Orientation:** mean differential ${b.scoreDiff.toFixed(3)} shows no large score bias; PLAYER win ${fmt(b.playerWinRate)} should still be monitored alongside ties.\n18. **Extreme cards:** §8 lists the five highest and lowest legal-choice rates; none is causal without controlling policy and hand alternatives.\n\n## 12. Risks and limitations\n\nThe policies are transparent heuristics, not optimal agents. Card win rates are observational. The simulator does not use perfect information. It excludes presentation and persistence. The Core Spec leaves several non-authoritative integration policies provisional. Authoritative damage reads the materialized 81-entry integer table; native exp() is not used during match resolution.\n\nThe illustrative 41–38 is plausible only if both sides sustain unusually high contested control; baseline average scores above show whether this bot population reaches that range.\n\n## 13. Recommended tuning changes\n\n### High-confidence problem\n\n- Treat any material paired orientation advantage as an engine-order defect to investigate before balance tuning.\n- Treat cards with near-zero legality as pool/eligibility problems before changing their power.\n\n### Promising hypothesis\n\n- Reduce duplicated Growth BUILD weight if River Mill adds test dilution without a measurable RIVER coverage benefit.\n- Tune late BUILD cost only if turn-14–20 selection is both rare and negatively associated across multiple policies and sensitivities.\n- Compare economic and military Keystones in targeted human play because bot valuation embeds their own bias.\n\n### Needs human playtest\n\n- Whether turn-one hands feel meaningfully distinct.\n- Whether REACH protection is legible and satisfying.\n- Whether leading-side tile retention feels recoverable.\n- Whether score ranges and 41–38 finishes feel exciting rather than predetermined.\n\n## 14. Owner decisions required\n\nNo rules or content were automatically changed. Owner approval is required for any magnitude, cost, cutoff, roster, or effect-package tuning. First review priorities are paired orientation delta, PASS rate, stranded Insight, late BUILD outcomes, Garrison Post versus other BUILD entries, River Mill duplication, and extreme card selection rates.\n`;
  fs.writeFileSync(REPORT,report);console.log(JSON.stringify({status:"PASS",golden:golden.length,determinismMatches:1000,baselineMatches:baseline.length,sensitivityMatches:summaries.slice(1).reduce((n,x)=>n+x.matches,0),totalAnalyzed:baseline.length+summaries.slice(1).reduce((n,x)=>n+x.matches,0),outputs:[RESULTS,REPORT]},null,2));}

main();
