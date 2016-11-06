<Query Kind="Statements">
  <Connection>
    <ID>4eb94a33-ed3f-4dfb-8755-87614302b79b</ID>
    <Persist>true</Persist>
    <Server>.</Server>
    <Database>VocaloidSite</Database>
  </Connection>
  <Output>DataGrids</Output>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Web</Namespace>
</Query>

var tagMappings = new Dictionary<string, string> {
{ "vocaloidアカペラ曲", "acappella" },
{ "ACOUSTIC", "acoustic" },
{ "オルタナティブミック", "alternative" },
{ "オルタナティブロック", "alternative rock" },
{ "アンビエント", "ambient" },
{ "AOR", "AOR" },
{ "ボカロバラード", "ballad" },
{ "VOCALOIDにゃんこ入り", "cats" },
{ "チップチューン", "chiptune" },
{ "Chiptune×VOCALOIDリンク", "chiptune" },
{ "クリスマス", "Christmas" },
{ "VOCALOIDクリスマス曲", "Christmas" },
{ "ミクラシック", "classical" },
{ "ボカロクラシカ", "classical" },
{ "VOCALOIDカバー曲", "cover" },
{ "VOCALOID電波ソング", "denpa" },
{ "電波", "denpa" },
{ "djent", "djent" },
{ "ミックンベース", "drum and bass" },
{ "ボカムンベース", "drum and bass" },
{ "ドラムステップ", "drumstep" },
{ "DubSteloid", "dubstep" },
{ "DubStep", "dubstep" },
{ "EDM", "EDM" },
{ "エレクトロハウス", "electro-house" },
{ "ElectroHouse", "electro-house" },
{ "ミクトロニカ", "electronica" },
{ "ユフトロニカ", "electronica" },
{ "ボカトロニカ", "electronica" },
{ "エレクトロニカ", "electronica" },
{ "エレクトロルカ", "electronica" },
{ "ボカロトロニカ", "electronica" },
{ "フォークトロニカ", "electronica" },
{ "UTAUトロニカ", "electronica" },
{ "VOCALOIDデジタルロック", "electro-rock" },
{ "ボカロ演歌", "enka" },
{ "Euphoric_Trance", "euphoric trance" },
{ "eurobeat", "eurobeat" },
{ "vocaloid-eurobeat", "eurobeat" },
{ "感性の反乱β", "experimental" },
{ "架空言語", "fictional language" },
{ "VOCALOID処女作", "first work" },
{ "VOCALOID民族調曲", "folk song" },
{ "ボカロ民謡", "folk song" },
{ "ボカロフュージョン", "fusion" },
{ "ガレージ", "garage rock" },
{ "ボカロガレージ", "garage rock" },
{ "ボカログランジ", "grunge" },
{ "ハードロック", "hard rock" },
{ "hard_trance", "hard trance" },
{ "ボカロハウス", "house" },
{ "VOCALOID幻想狂気曲リンク", "insane fantasy" },
{ "Lat式ミク", "Lat Miku" },
{ "ボカロメタル", "metal" },
{ "MMD", "MMD" },
{ "MMD-PV", "MMD" },
{ "ネオクラシカル", "neo-classical" },
{ "ミクノイズ", "noise" },
{ "VocaNoise", "noise" },
{ "ピアノミク", "piano" },
{ "MikuPOP", "pop" },
{ "VOCAPOP", "pop" },
{ "ポストミック", "post-rock" },
{ "ポストロック", "post-rock" },
{ "プログレ", "progressive rock" },
{ "プログロイド", "progressive rock" },
{ "プログレミク", "progressive rock" },
{ "サイケデミックトランス", "psy trance" },
{ "ボカロラップ", "rap" },
{ "R&B", "RnB" },
{ "VOCAR&B", "RnB" },
{ "ﾎﾞｶﾛｯｸﾊﾞﾗｰﾄﾞ", "rock ballad" },
{ "ミクゲイザー", "shoegaze" },
{ "VOCALOID雪曲", "snow" },
{ "ボカロシンフォニー", "symphonic" },
{ "テクノ", "techno" },
{ "ミクノ", "techno" },
{ "ミクノポップ", "techno" },
{ "テクノロック", "technorock" },
{ "VOCALOID和風曲", "traditional" },
{ "ミクトランス", "trance" },
{ "ボカロトランス", "trance" },
{ "元気が出るミクうた", "uplifting" },
{ "元気が出る曲", "uplifting" },
{ "ファンク", "funk" },
{ "ボカロファンク", "funk" },
{ "VOCAJAZZ", "jazz" },
{ "VOCALEAMO", "VOCALEAMO" },
{ "vocaloud", "VOCALOUD" },
{ "VOCAPUNK", "punk" },
{ "UTAUROCK", "rock" },
{ "VOCAROCK", "rock" },
{ "VOCASKA", "ska" },
{ "ボカロワルツ", "waltz" },
{ "VOCALOID冬曲", "winter" }
};

var tagsByName = TagNames.ToArray().ToDictionary(t => t.Value, t => t.Tag, StringComparer.InvariantCultureIgnoreCase);
//tagMappings.Where(t => !tagsByName.ContainsKey(t.Value)).Dump();
var tagMappingsWithTags = tagMappings.Where(t => tagsByName.ContainsKey(t.Value)).Select(t => new TagMappings { SourceTag = t.Key, Tag = tagsByName[t.Value] }).ToArray();
tagMappingsWithTags.Dump();

TagMappings.InsertAllOnSubmit(tagMappingsWithTags);
SubmitChanges();