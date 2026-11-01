using UnityEngine;

// Общие настройки управления. Меню настроек пишет сюда, остальные скрипты читают.
public static class ControlSettings
{
    public static bool leftHanded = false;        // режим левши: джойстик и камера меняются местами
    public static bool vibration = true;
    public static float lookSensitivity = 0.15f;  // градусов на пиксель свайпа
    public static float masterVolume = 1f;

    public static void Load()
    {
        leftHanded = PlayerPrefs.GetInt("s_left", 0) == 1;
        vibration = PlayerPrefs.GetInt("s_vib", 1) == 1;
        lookSensitivity = PlayerPrefs.GetFloat("s_sens", 0.15f);
        masterVolume = PlayerPrefs.GetFloat("s_vol", 1f);
    }

    public static void Save()
    {
        PlayerPrefs.SetInt("s_left", leftHanded ? 1 : 0);
        PlayerPrefs.SetInt("s_vib", vibration ? 1 : 0);
        PlayerPrefs.SetFloat("s_sens", lookSensitivity);
        PlayerPrefs.SetFloat("s_vol", masterVolume);
        PlayerPrefs.Save();
    }
}
