using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

public class FirebaseAccountAuthService
{
    public FirebaseUser CurrentUser { get; private set; }
    public string UserId => CurrentUser != null ? CurrentUser.UserId : string.Empty;

    public async Task<bool> InitializeAndSignInAsync()
    {
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogWarning("Firebase dependencies are not available: " + status);
            return false;
        }

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        CurrentUser = auth.CurrentUser;

        if (CurrentUser == null)
        {
            AuthResult result = await auth.SignInAnonymouslyAsync();
            CurrentUser = result.User;
        }

        Debug.Log("Firebase signed in. UID: " + UserId);
        return CurrentUser != null;
    }
}
