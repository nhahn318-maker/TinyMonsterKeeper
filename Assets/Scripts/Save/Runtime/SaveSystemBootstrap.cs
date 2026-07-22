using System;
using UnityEngine;

public class SaveSystemBootstrap : MonoBehaviour
{
    public static SaveManager SaveManager { get; private set; }

    [SerializeField] private bool useCloudSave = true;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool logSaveState = true;

    private async void Awake()
    {
        if (SaveManager != null)
        {
            Destroy(gameObject);
            return;
        }

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        ISaveService cloudService = useCloudSave ? new FirebaseCloudSaveService() : null;
        SaveManager = new SaveManager(cloudService);

        try
        {
            await SaveManager.InitializeAsync();
            if (logSaveState)
            {
                string mode = SaveManager.IsCloudReady ? "cloud" : "local";
                Debug.Log($"Save system ready ({mode}). UserId: {SaveManager.UserId}");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("Save system failed to initialize: " + exception);
        }
    }
}
