﻿#define ALLOW_HIRAGANA
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection.Emit;
using System.Linq;

public class Utils
{
    public static int saveFile = 0;
    public static string username = "";
    public static string currentLanguage = "null";

    public static string enemyToBattle = "null";
    public static int enemyToBattleIndex = -1;

    public static List<string> enemyNames = new List<string>();
    public static List<Vector3> enemyPos = new List<Vector3>();

    public static string saveToFile = "null";

    public static int numberOfLearnedHiragana = 0;
    public static List<Pair<string, float>> kanaPercentageLearned = new List<Pair<string, float>>();

    public static int backgroundUsed = 1;
    
    public static float yLimit = -10f;

    private static List<Pair<string, string>> kanaRows = new List<Pair<string, string>>() { new Pair<string, string>("hiraganavowels", "あいうえお"),
                                                                                            new Pair<string, string>("hiraganak", "かきくけこ"),
                                                                                            new Pair<string, string>("katakanavowels", "アイウエオ"),
                                                                                            new Pair<string, string>("katakanak", "カキクケコ")};

    private static Dictionary<string, string> kanaToRomaji = new Dictionary<string, string>() {
        { "あ", "a" },
        { "い", "i" },
        { "う", "u" },
        { "え", "e" },
        { "お", "o" },
        { "か", "ka" },
        { "き", "ki" },
        { "く", "ku" },
        { "け", "ke" },
        { "こ", "ko" },
        { "ア", "a" },
        { "イ", "i" },
        { "ウ", "u" },
        { "エ", "e" },
        { "オ", "o" },
        { "カ", "ka" },
        { "キ", "ki" },
        { "ク", "ku" },
        { "ケ", "ke" },
        { "コ", "ko" }
    };

    public static string wantedChar = "";

    public static Vector3 GetMouseWorldPosition() {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;
        return worldPos;
    }

    public static bool ApproximatelyEqual(float a, float b, float errorAllowed) {
        return (a - errorAllowed <= b && b <= a + errorAllowed);
    }

    public static Vector2 GetTouchWorldPosition(Touch touch) {
        return Camera.main.ScreenToWorldPoint(touch.position);
    }

    public static bool CheckPointIsWithinRadius(Vector2 centerPoint, Vector2 pointToCheck, double radius) {
        return radius >= Math.Sqrt((Math.Pow(centerPoint.x - pointToCheck.x, 2) + 
                                    Math.Pow(centerPoint.y - pointToCheck.y, 2)));
    }

    public static void InitGameData() {
        string dirPath = Application.persistentDataPath + "/item_data";
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        InitUnitData();
        InitEnemyData();
        InitLevelData();
        InitKanaData();
    }

    public static void SaveGameData() {
        SaveTeamData();
    }

    public static void LoadGameData() {
        LoadTeamData();
    }

    public static string PrepareWantedChar(bool allowHiragana, bool allowKatakana) {
        if (wantedChar != "") {
            return wantedChar;
        }

        int count = 0;
        if (allowHiragana) {
            count++;
        }
        if (allowKatakana) {
            count++;
        }

        if (count == 0) {
            return "";
        }

        SortedList<string, float> possibleChars = new SortedList<string, float>();
        foreach (Pair<string, float> pair in kanaPercentageLearned) {
            possibleChars.Add(pair.First, pair.Second);
        }

        SpacedRepetitionImplementation(ref possibleChars, UnityEngine.Random.Range(0f, 1f) < 0.75f);

        return wantedChar;
    }

    private static void SpacedRepetitionImplementation(ref SortedList<string, float> possibleChars, bool selectLowestValue) {
        List<KeyValuePair<string, float>> ordered = possibleChars.OrderBy(pair => pair.Value).ToList();

        float lowestValue = ordered[0].Value;
        int amountWithLowestValue = 1;

        for (int i = 1; i < ordered.Count; i++) {
            if (ordered[i].Value == lowestValue) {
                amountWithLowestValue++;
            } else {
                break;
            }
        }

        if (selectLowestValue || amountWithLowestValue == ordered.Count) {
            wantedChar = ordered[UnityEngine.Random.Range(0, amountWithLowestValue)].Key;

            foreach(Pair<string, string> kanaRow in kanaRows) {
                if (kanaRow.Second.IndexOf(wantedChar) != -1) { // if wanted character is in this row
                    currentLanguage = kanaRow.First; // set the current language to this row

                    if (kanaRows.IndexOf(kanaRow) < kanaRows.Count / 2) { // if we found this in the first half, it's a hiragana row
                        wantedChar += " in hiragana.";
                    } else { // otherwise it's a katakana row
                        wantedChar += " in katakana.";
                    }
                }
            }
        } else {
            SortedList<string, float> newList = new SortedList<string, float>();

            for (int i = amountWithLowestValue; i < ordered.Count; i++) {
                newList.Add(ordered[i].Key, ordered[i].Value);
            }

            possibleChars = newList;
            SpacedRepetitionImplementation(ref possibleChars, UnityEngine.Random.Range(0f, 1f) < 0.75f);
        }
    }

    private static void InitKanaData() {
        string filePath = Application.persistentDataPath + "/save_data_" + saveFile + ".dat";
        FileStream file;

        if (!File.Exists(filePath)) {
            Debug.LogError(filePath + " does not exist! Can't write kana data!");
            return;
        }

        file = File.OpenRead(filePath);

        BinaryFormatter bf = new BinaryFormatter();
        ArrayList data = (ArrayList)bf.Deserialize(file);
        file.Close();

        int index = -1;
        for (int i = 0; i < data.Count; i++) {
            if (((string)data[i]).Contains("[Hiragana_Number]: ")) {
                index = i;
            }
        }

        if (index == -1) {
            string dataToWrite = "[Hiragana_Number]: 0";

            file = File.OpenWrite(filePath);

            data.Add(dataToWrite);
            data.Add("");

            bf.Serialize(file, data);
            file.Close();
        } else {
            string substring = (string)data[index];
            substring = substring.Substring(substring.IndexOf(':') + 2);
            numberOfLearnedHiragana = int.Parse(substring);

            if (numberOfLearnedHiragana > 0) {
                string kanaSubstring = (string)data[index + 1];

                for (int i = 0; i < numberOfLearnedHiragana; i++) {
                    Pair<string, float> newPair = new Pair<string, float>();
                    newPair.First = kanaSubstring.Substring(0, kanaSubstring.IndexOf(':'));
                    newPair.Second = float.Parse(kanaSubstring.Substring(kanaSubstring.IndexOf(':') + 2, kanaSubstring.IndexOf('\n') - kanaSubstring.IndexOf(':') - 2));

                    kanaPercentageLearned.Add(newPair);

                    if (i != numberOfLearnedHiragana - 1) {
                        kanaSubstring = kanaSubstring.Substring(kanaSubstring.IndexOf('\n') + 1); // offset to next entry
                    }
                }
            }
        }
    }

    public static void SaveKanaData() {
        string filePath = Application.persistentDataPath + "/save_data_" + saveFile + ".dat";
        FileStream file;

        if (!File.Exists(filePath)) {
            Debug.LogError(filePath + " does not exist! Can't write kana data!");
            return;
        }

        file = File.OpenRead(filePath);

        BinaryFormatter bf = new BinaryFormatter();
        ArrayList data = (ArrayList)bf.Deserialize(file);
        file.Close();

        int index = -1;
        for (int i = 0; i < data.Count; i++) {
            if (((string)data[i]).Contains("[Hiragana_Number]: ")) {
                index = i;
            }
        }

        if (index == -1) {
            Debug.LogError("Kana data has not been initialized! Reload game to fix issue. Any previous data has been lost.");
            return;
        }

        string substring = (string)data[index];
        substring = substring.Replace(substring.Substring(substring.IndexOf(':') + 2), numberOfLearnedHiragana.ToString());
        data[index] = substring;

        string dataToWrite = "";
        foreach(Pair<string, float> pair in kanaPercentageLearned) {
            string newData = pair.First + ": " + pair.Second + "\n";
            dataToWrite += newData;
        }
        data[index + 1] = dataToWrite;

        file = File.OpenWrite(filePath);

        bf.Serialize(file, data);
        file.Close();
        Debug.Log(dataToWrite);
    }

    private static void LoadKanaData() {
        string filePath = Application.persistentDataPath + "/save_data_" + saveFile + ".dat";
        FileStream file;

        if (!File.Exists(filePath)) {
            Debug.LogError(filePath + " does not exist! Can't write kana data!");
            return;
        }

        file = File.OpenRead(filePath);

        BinaryFormatter bf = new BinaryFormatter();
        ArrayList data = (ArrayList)bf.Deserialize(file);
        file.Close();

        int index = -1;
        for (int i = 0; i < data.Count; i++) {
            if (((string)data[i]).Contains("[Hiragana_Number]: ")) {
                index = i;
            }
        }
        
        string substring = (string)data[index];
        substring = substring.Substring(substring.IndexOf(':') + 2);
        numberOfLearnedHiragana = int.Parse(substring);

        if (numberOfLearnedHiragana > 0) {
            string kanaSubstring = (string)data[index + 1];
            kanaSubstring = kanaSubstring.Substring(kanaSubstring.IndexOf('\n') + 1); // offset to first entry

            for (int i = 0; i < numberOfLearnedHiragana; i++) {
                Pair<string, float> newPair = new Pair<string, float>();
                newPair.First = kanaSubstring.Substring(0, kanaSubstring.IndexOf(':'));
                newPair.Second = float.Parse(kanaSubstring.Substring(kanaSubstring.IndexOf(':') + 2, kanaSubstring.IndexOf('\n') - kanaSubstring.IndexOf(':') - 2));

                kanaPercentageLearned.Add(newPair);
            }
        }
    }

    public static void AddKanaData(string kana) {
        foreach (Pair<string, float> pair in kanaPercentageLearned) {
            if (pair.First == kana) {
                return;
            }
        }

        kanaPercentageLearned.Add(new Pair<string, float>(kana, 0f));
        numberOfLearnedHiragana = kanaPercentageLearned.Count;
    }

    public static void ModifyKanaData(string kana, float value) {
        foreach (Pair<string, float> pair in kanaPercentageLearned) {
            if (pair.First == kana) {
                pair.Second = Mathf.Max(Mathf.Min(pair.Second + value, 100f), 0f);
                break;
            }
        }

        SaveKanaData();
    }

    private static void SaveTeamData() {
        string filePath = Application.persistentDataPath + "/team_data_" + saveFile + ".dat";
        FileStream file;

        if (File.Exists(filePath) == true) {
            file = File.OpenWrite(filePath);
        } else {
            file = File.Create(filePath);
        }

        ArrayList data = Team_Data.GetData();

        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(file, data);
        file.Close();
    }

    private static void LoadTeamData() {
        string filePath = Application.persistentDataPath + "/team_data_" + saveFile + ".dat";
        FileStream file;

        if (File.Exists(filePath) == false) {
            Debug.LogError("No file found! Function: LoadTeamData");
            return;
        }

        file = File.OpenRead(filePath);
        
        BinaryFormatter bf = new BinaryFormatter();
        ArrayList data = (ArrayList)bf.Deserialize(file);
        file.Close();

        Team_Data.InitData((List<string>) data[0], 
                        (List<Battle_Entity_Stats>) data[1], 
                        (List<Battle_Entity_Loadout>) data[2], 
                        (List<List<Battle_Entity_Spells>>) data[3],
                        (List<Item>) data[4]);
    }

    private static void WriteOneLineToFile(string filePath, string line) {
        using (FileStream file = new FileStream(filePath, FileMode.Append, FileAccess.Write))

        using (StreamWriter sw = new StreamWriter(file)) {
            sw.WriteLine(line);
        }
    }

    public static string KanaToRomaji(string kana) {
        return kanaToRomaji[kana[0].ToString()] + kana.Substring(1);
    }

    private static void InitUnitData() {
        string filePath = Application.persistentDataPath + "/team_data_" + saveFile + ".dat";
        FileStream file;

        if (File.Exists(filePath) == true) {
            LoadTeamData();
            return;
        }

        Battle_Entity_Stats unitStats = new Battle_Entity_Stats(1,   // level
                                                0,    // currXP
                                                100,  // maxXP
                                                100,  // currHP
                                                100,  // maxHP
                                                100,  // currMana
                                                100,  // maxMana
                                                20,   // strength
                                                10,   // magic
                                                10,   // speed
                                                10,   // defense
                                                10); // resistance);

        List<Battle_Entity_Spells> spells = new List<Battle_Entity_Spells> {
            new Fireball(),
            new Frostbite(),
            new Lightning(),
            new Shadow(),
            new HolyLight()
        };

        Team_Data.AddNewEntry(username, unitStats, new Battle_Entity_Loadout(), spells);

        for (int i = 0; i < 3; i++) {
            Team_Data.AddItem(new Potion());
            Team_Data.AddItem(new Mana_Potion());
            Team_Data.AddItem(new Scroll_of_Strength());
            Team_Data.AddItem(new Scroll_of_Magic());
            Team_Data.AddItem(new Scroll_of_Protection());
        }

        SaveTeamData();
    }

    private static void InitEnemyData() {
        string filePath = Application.persistentDataPath + "/enemy_data.dat";
        FileStream file;

        if (File.Exists(filePath) == true) {
            file = File.OpenWrite(filePath);
        } else {
            file = File.Create(filePath);
        }

        // Format: "Enemy_Name: sizeX sizeY offsetX offsetY"
        string dataString = "Flower: +0.66 +1.23 +0.00 -0.39\n" +
                            "Masked Doctor: +0.94 +1.26 -0.28 -0.37\n" +
                            "Cavern Monster: +0.57 +0.60 -0.12 -0.20\n" +
                            "Serpent: +0.50 +1.67 -0.34 -0.14";

        ArrayList data = new ArrayList() { dataString };

        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(file, data);
        file.Close();
    }

    public static List<float> LoadEnemyData(string enemyName) {
        string filePath = Application.persistentDataPath + "/enemy_data.dat";
        FileStream file;

        if (File.Exists(filePath) == true) {
            file = File.OpenRead(filePath);
        } else {
            Debug.LogError("No enemy_data.dat file found!");
            return null;
        }


        BinaryFormatter bf = new BinaryFormatter();

        ArrayList data = (ArrayList)bf.Deserialize(file);
        file.Close();

        string dataString = (string)data[0];

        if (!dataString.Contains(enemyName)) {
            Debug.LogError("No enemy named " + enemyName + " found!");
            return null;
        }

        List<float> list = new List<float>(4);

        for (int i = 0; i < 4; i++) { 
            list.Add(float.Parse(dataString.Substring(dataString.IndexOf(enemyName) + enemyName.Length + 2 + i * 6, 5)));
        }

        return list;
    }

    public static void SavePlayerPosition(Vector3 playerPos) {
        string filePath = Application.persistentDataPath + "/save_data_" + saveFile + ".dat";
        FileStream file;

        if (!File.Exists(filePath)) {
            Debug.LogError(filePath + " does not exist! Can't write player position!");
            return;
        }

        file = File.OpenRead(filePath);

        BinaryFormatter bf = new BinaryFormatter();
        ArrayList data = (ArrayList)bf.Deserialize(file);
        file.Close();

        int index = -1;
        for (int i = 0; i < data.Count; i++) {
            if (((string)data[i]).Contains("[Position]: ")) {
                index = i;
            }
        }

        string dataToWrite = null;
        if (index == -1) {
            dataToWrite = "[Position]: " + playerPos.x + ", " + playerPos.y + ", " + playerPos.z + ",";
            data.Add(dataToWrite);
        } else {
            dataToWrite = ((string)data[index]).Replace((string)data[index], 
                                            "[Position]: " + playerPos.x + ", " + playerPos.y + ", " + playerPos.z + ",");
            data[index] = dataToWrite;
        }

        file = File.OpenWrite(filePath);

        bf.Serialize(file, data);
        file.Close();
    }

    public static Vector3 LoadPlayerPosition() {
        Vector3 playerPos = Vector3.zero;

        string filePath = Application.persistentDataPath + "/save_data_" + saveFile + ".dat";
        FileStream file;

        if (!File.Exists(filePath)) {
            Debug.LogError(filePath + " does not exist! Can't write player position!");
            return playerPos;
        }

        file = File.OpenRead(filePath);

        BinaryFormatter bf = new BinaryFormatter();
        ArrayList data = (ArrayList)bf.Deserialize(file);
        file.Close();

        int index = -1;
        for (int i = 0; i < data.Count; i++) {
            if (((string)data[i]).Contains("[Position]: ")) {
                index = i;
            }
        }

        if (index != -1) {
            string dataRead = (string)data[index];

            playerPos = ParsePosition(dataRead);
            Debug.Log(playerPos);
        }

        return playerPos;
    }

    public static void InitLevelData() {
        string filePath = Application.persistentDataPath + "/level_data.dat";
        FileStream file;

        if (File.Exists(filePath) == true) {
            file = File.OpenWrite(filePath);
        } else {
            file = File.Create(filePath);
        }

        string levelData = "[Level]: 1\n";
        string enemyData = "[Enemies]:\n" +
                            "Flower: +60.00, -1.00,\n" +
                            "Serpent: +120.00, +8.00,\n" +
                            "Cavern Monster: +225.00, -59.00,\n" +
                            "Masked Doctor: +255.00, -57.00,\n" +
                            "Flower: +255.00, -49.00,\n" +
                            "Flower: +321.00, -55.00,\n" +
                            "Cavern Monster: +332.00, -51.00,\n" +
                            "Serpent: +341.00, -47.00,\n" +
                            "Masked Doctor: +330.00, -43.00,\n" +
                            "Flower: +261.00, -33.00,\n" +
                            "Masked Doctor: +100.00, +13.00,\n" + 
                            "Flower: +139.00, +3.00,\n";
        string playerData = "[Player]: -3.00, -1.00,";

        ArrayList levels = new ArrayList() { levelData, enemyData, playerData };

        ArrayList data = new ArrayList() { levels };

        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(file, data);
        file.Close();
    }

    public static void LoadLevelData(int level) {
        string filePath = Application.persistentDataPath + "/level_data.dat";
        FileStream file;

        if (!File.Exists(filePath)) {
            Debug.LogError(filePath + " does not exist! Can't load level data!");
            return;
        }

        file = File.OpenRead(filePath);

        BinaryFormatter bf = new BinaryFormatter();
        ArrayList data = (ArrayList)bf.Deserialize(file);
        file.Close();

        ArrayList levelsData = (ArrayList)data[0];

        for (int i = 0; i < levelsData.Count / 3; i++) {
            string levelNumber = (string)levelsData[i];
            
            if (int.Parse(levelNumber.Substring(levelNumber.LastIndexOf(':') + 2)) == level) {
                string playerData = (string)levelsData[i + 2];
                SavePlayerPosition(ParsePosition(playerData));
                
                string enemyData = (string)levelsData[i + 1];

                enemyData = enemyData.Substring(enemyData.IndexOf('\n') + 1);
                while (enemyData.Contains(":")) {
                    enemyNames.Add(enemyData.Substring(0, enemyData.IndexOf(':')));
                    enemyPos.Add(ParsePosition(enemyData.Substring(enemyData.IndexOf(':') + 1, enemyData.IndexOf('\n') - enemyData.IndexOf(':') - 1)));

                    Debug.Log(enemyNames[enemyNames.Count - 1] + ": " + enemyPos[enemyPos.Count - 1]);
                    enemyData = enemyData.Substring(enemyData.IndexOf('\n') + 1);
                }
            }
        }
    }

    // string form: "Name: xCoords, yCoords, [zCoords,]"
    public static Vector3 ParsePosition(string str) {
        Vector3 pos = Vector3.zero;

        pos.x = float.Parse(str.Substring(str.IndexOf(':') + 2, str.IndexOf(',') - str.IndexOf(':') - 2));

        str = str.Substring(str.IndexOf(',') + 2);

        pos.y = float.Parse(str.Substring(0, str.IndexOf(',')));

        str = str.Substring(str.IndexOf(',') + 1);

        if (str.Contains(",")) {
            pos.z = float.Parse(str.Substring(0, str.IndexOf(',')));
        }

        return pos;
    }
}
