using System;
using TMPro;
using UnityEngine;

public enum FarmBoxMergeLanguage { English, Turkish, Spanish, Chinese }

[CreateAssetMenu(fileName = "FarmBoxMergeLocalizationCatalog", menuName = "FarmBoxMerge/Localization Catalog")]
public sealed class FarmBoxMergeLocalizationCatalog : ScriptableObject
{
    [Serializable]
    public sealed class LanguageFont
    {
        public FarmBoxMergeLanguage language;
        public TMP_FontAsset font;
        public LanguageFont(FarmBoxMergeLanguage language) { this.language = language; }
    }

    [Serializable]
    public sealed class Translation
    {
        public string key;
        [TextArea] public string english;
        [TextArea] public string turkish;
        [TextArea] public string spanish;
        [TextArea] public string chinese;
        public Translation(string key, string english, string turkish, string spanish, string chinese)
        {
            this.key = key; this.english = english; this.turkish = turkish;
            this.spanish = spanish; this.chinese = chinese;
        }
        public string Get(FarmBoxMergeLanguage language) => language switch
        {
            FarmBoxMergeLanguage.Turkish => turkish,
            FarmBoxMergeLanguage.Spanish => spanish,
            FarmBoxMergeLanguage.Chinese => chinese,
            _ => english
        };
    }

    [Tooltip("Assign a TMP font asset per language. Unassigned fonts retain the scene's original font.")]
    public LanguageFont[] fonts =
    {
        new LanguageFont(FarmBoxMergeLanguage.English), new LanguageFont(FarmBoxMergeLanguage.Turkish),
        new LanguageFont(FarmBoxMergeLanguage.Spanish), new LanguageFont(FarmBoxMergeLanguage.Chinese)
    };

    public Translation[] translations =
    {
        new Translation("play", "PLAY", "OYNA", "JUGAR", "开始游戏"),
        new Translation("settings", "SETTINGS", "AYARLAR", "AJUSTES", "设置"),
        new Translation("sound", "SOUND", "SES", "SONIDO", "声音"),
        new Translation("haptics", "VIBRATION", "TİTREŞİM", "VIBRACIÓN", "振动"),
        new Translation("cancel", "CLOSE", "KAPAT", "CERRAR", "关闭"),
        new Translation("language", "LANGUAGE", "DİL", "IDIOMA", "语言"),
        new Translation("add_card", "ADD CARD", "KART EKLE", "AÑADIR CARTA", "添加卡牌"),
        new Translation("trash", "TRASH", "ÇÖP", "BASURA", "丢弃"),
        new Translation("refresh", "REFRESH", "YENİLE", "RENOVAR", "刷新"),
        new Translation("retry", "RETRY", "TEKRAR", "REINTENTAR", "重试"),
        new Translation("retry_level", "RETRY LEVEL", "TEKRAR OYNA", "REINTENTAR", "重玩关卡"),
        new Translation("next_level", "NEXT LEVEL", "SONRAKİ BÖLÜM", "SIGUIENTE NIVEL", "下一关"),
        new Translation("win", "HARVEST COMPLETE!", "HASAT TAMAMLANDI!", "¡COSECHA COMPLETA!", "收获完成！"),
        new Translation("fail", "TRY AGAIN!", "TEKRAR DENE!", "¡INTÉNTALO DE NUEVO!", "再试一次！"),
        new Translation("level", "LEVEL {0}", "BÖLÜM {0}", "NIVEL {0}", "第 {0} 关"),
        new Translation("items_left", "ITEMS\nLEFT", "KALAN\nÜRÜN", "PRODUCTOS\nRESTANTES", "剩余\n物品"),
        new Translation("green", "GREEN", "YEŞİL", "VERDE", "绿色"),
        new Translation("orange", "ORANGE", "TURUNCU", "NARANJA", "橙色"),
        new Translation("purple", "PURPLE", "MOR", "MORADO", "紫色"),
        new Translation("red", "RED", "KIRMIZI", "ROJO", "红色"),
        new Translation("yellow", "YELLOW", "SARI", "AMARILLO", "黄色"),
        new Translation("ad", "AD", "REKLAM", "ANUNCIO", "广告"),
        new Translation("merge_title", "MERGE CARDS", "KARTLARI BİRLEŞTİR", "FUSIONA CARTAS", "合并卡牌"),
        new Translation("merge_rules", "MERGE CARDS\n<size=58%>1 + 1 = 2   •   2 + 2 = 3   •   3 + 3 = 4</size>", "KARTLARI BİRLEŞTİR\n<size=58%>1 + 1 = 2   •   2 + 2 = 3   •   3 + 3 = 4</size>", "FUSIONA CARTAS\n<size=58%>1 + 1 = 2   •   2 + 2 = 3   •   3 + 3 = 4</size>", "合并卡牌\n<size=58%>1 + 1 = 2   •   2 + 2 = 3   •   3 + 3 = 4</size>"),
        new Translation("tutorial_merge", "1 / 2   MERGE MATCHING CARDS\n<size=72%>Drag one card onto the other: 1 + 1 = 2</size>", "1 / 2   AYNI KARTLARI BİRLEŞTİR\n<size=72%>Bir kartı diğerine sürükle: 1 + 1 = 2</size>", "1 / 2   FUSIONA CARTAS IGUALES\n<size=72%>Arrastra una carta sobre la otra: 1 + 1 = 2</size>", "1 / 2   合并相同卡牌\n<size=72%>将一张卡牌拖到另一张上：1 + 1 = 2</size>"),
        new Translation("tutorial_place", "2 / 2   MAKE YOUR FIRST BOX\n<size=72%>Drag the 2 card onto a 2-box shape</size>", "2 / 2   İLK KUTUNU YERLEŞTİR\n<size=72%>2 kartını ikili kutu alanına sürükle</size>", "2 / 2   COLOCA TU PRIMERA CAJA\n<size=72%>Arrastra la carta 2 a una forma de 2 cajas</size>", "2 / 2   放置第一组箱子\n<size=72%>将数字2的卡牌拖到双箱区域</size>"),
        new Translation("hint_drag", "DRAG {0} TO A MATCHING {0}-BOX SHAPE", "{0} KARTINI {0} KUTULU ALANA SÜRÜKLE", "ARRASTRA {0} A UNA FORMA DE {0} CAJAS", "将{0}卡牌拖到{0}箱区域"),
        new Translation("hint_match", "MATCH BOTH COLOR AND NUMBER", "RENK VE SAYI AYNI OLMALI", "EL COLOR Y EL NÚMERO DEBEN COINCIDIR", "颜色和数字都必须相同"),
        new Translation("hint_merged", "MATCH! TWO {0} CARDS MAKE {1}", "HARİKA! İKİ {0} KARTI {1} OLUR", "¡BIEN! DOS CARTAS {0} HACEN {1}", "成功！两张{0}卡牌合成{1}"),
        new Translation("hint_next", "NICE! CHECK ITEMS LEFT FOR YOUR NEXT BOX", "GÜZEL! SONRAKİ KUTU İÇİN KALAN ÜRÜNLERE BAK", "¡BIEN! MIRA LOS PRODUCTOS RESTANTES", "很好！根据剩余物品规划下一组箱子"),
        new Translation("hint_busy", "THAT SPACE IS BUSY — TRY AN EMPTY SHAPE", "BU ALAN DOLU — BOŞ BİR ALAN DENE", "ESE ESPACIO ESTÁ OCUPADO — PRUEBA OTRO", "该区域已被占用，请选择空闲区域"),
        new Translation("hint_shape", "THIS SHAPE NEEDS A {0} CARD", "BU ALANA {0} KARTI GEREKİYOR", "ESTA FORMA NECESITA UNA CARTA {0}", "此区域需要数字{0}的卡牌")
    };

    public TMP_FontAsset GetFont(FarmBoxMergeLanguage language)
    {
        foreach (LanguageFont entry in fonts)
            if (entry != null && entry.language == language) return entry.font;
        return null;
    }
}
