using UnityEngine;

public class SaveAccountResetTool : MonoBehaviour
{
    [SerializeField] private bool clearAllTinyMonsterKeeperPlayerPrefs = true;
    [SerializeField] private bool reloadSceneAfterReset = true;

    [ContextMenu("Reset Current Account Save")]
    public async void ResetCurrentAccountSave()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode before resetting cloud save so Firebase has a signed-in account.");
            return;
        }

        if (clearAllTinyMonsterKeeperPlayerPrefs)
            ClearTinyMonsterKeeperPlayerPrefs();

        SaveManager saveManager = SaveSystemBootstrap.SaveManager;
        if (saveManager == null || !saveManager.IsReady)
        {
            Debug.LogWarning("SaveManager is not ready yet. Wait for Save system ready log, then reset again.");
            return;
        }

        await saveManager.ResetCurrentSaveAsync();
        Debug.Log("Current account save reset. UserId: " + saveManager.UserId);

        if (reloadSceneAfterReset)
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private static void ClearTinyMonsterKeeperPlayerPrefs()
    {
        // Unity cannot enumerate keys, so clear the whole dev profile during test resets.
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
