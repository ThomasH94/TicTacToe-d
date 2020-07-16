using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;

/// <summary>
/// This script has to be manually created but the code comes from Playfab themselves
/// See the documentation here:
/// https://docs.microsoft.com/en-us/gaming/playfab/sdks/unity3d/quickstart
/// https://www.infogamerhub.com/how-to-use-playfab-in-unity-3d-login-lesson-2/
/// </summary>
public class PlayFabController : MonoBehaviour
{
    public static PlayFabController Instance;
    
    private string userEmail;
    private string userPassword;
    private string userName;

    public GameObject loginPanel;
    public GameObject addLoginPanel;
    public GameObject recoverAccountButton;

    private void OnEnable()
    {
        if (PlayFabController.Instance == null)
        {
            PlayFabController.Instance = this;
        }
        else
        {
            if (PlayFabController.Instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        //Note: Setting title Id here can be skipped if you have set the value in Editor Extensions already.
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId)){
            PlayFabSettings.TitleId = "4F74A"; // Please change this value to your own titleId from PlayFab Game Manager
        }
        
        PlayerPrefs.DeleteAll();    // For testing
        /* Used for testing, use a valid way to verify accounts!
        var request = new LoginWithCustomIDRequest { CustomId = "GettingStartedGuide", CreateAccount = true};
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
        */

        if (PlayerPrefs.HasKey("EMAIL"))
        {
            userEmail = PlayerPrefs.GetString("EMAIL");
            userPassword = PlayerPrefs.GetString("PASSWORD");
            var request = new LoginWithEmailAddressRequest {Email = userEmail, Password = userPassword};
            PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);    // Send in our request, and what to do if it passes/fails   
        }
        else
        {
#if UNITY_ANDROID
            // NOTE: This will look greyed out in our IDE if we aren't on the target platform in the build settings
            // Register the account via the device ID automatically
            var requestAndroid = new LoginWithAndroidDeviceIDRequest {AndroidDeviceId = ReturnMobileID(), CreateAccount = true};
            PlayFabClientAPI.LoginWithAndroidDeviceID(requestAndroid, OnLoginMobileSuccess, OnLoginMobileFailure);
#endif

#if UNITY_IOS
            var requestIOS = new LoginWithIOSDeviceIDRequest {DeviceId = ReturnMobileID(), CreateAccount = true};
            PlayFabClientAPI.LoginWithIOSDeviceID(requestIOS, OnLoginMobileSuccess, OnLoginMobileFailure);
#endif
        }
    }

    public static string ReturnMobileID()
    {
        string deviceID = SystemInfo.deviceUniqueIdentifier;    // Get the device we are on and it's ID
        return deviceID;
    }

    private string ReturnAString()
    {
        return "s";
    }

    #region Login
    // TODO: Move all Menu related functionality to it's own Menu Controller class
    
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Login Successful!");
        PlayerPrefs.SetString("EMAIL", userEmail);    // Temp way to store the email of the user so they don't have to constantly login
        PlayerPrefs.SetString("PASSWORD", userPassword);
        loginPanel.SetActive(false);
        recoverAccountButton.SetActive(false);
        GetStats();    // Load our stats
    }
    
    private void OnLoginMobileSuccess(LoginResult result)
    {
        Debug.Log("Login for Mobile Successful!");
        GetStats();
        loginPanel.SetActive(false);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log("Successful registered a player!");
        PlayerPrefs.SetString("EMAIL", userEmail);    // Temp way to store the email of the user so they don't have to constantly login
        PlayerPrefs.SetString("PASSWORD", userPassword);
        GetStats();    // Load our stats
        loginPanel.SetActive(false);
    }

    private void OnLoginFailure(PlayFabError error)
    {
        var registerRequest = new RegisterPlayFabUserRequest() {Email = userEmail, Password = userPassword, Username = userName};
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, OnRegisterSuccess, OnRegisterFailure);
    }
    
    private void OnLoginMobileFailure(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }

    private void OnRegisterFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());    // Let the user know why the registration failed
    }

    public void GetUserEmail(string emailIn)
    {
        userEmail = emailIn;
    }

    public void GetUserPassword(string passwordIn)
    {
        userPassword = passwordIn;
    }

    public void GetUsername(string userNameIn)
    {
        userName = userNameIn;
    }

    public void OnClickLogin()
    {
        var request = new LoginWithEmailAddressRequest {Email = userEmail, Password = userPassword};
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);    // Send in our request, and what to do if it passes/fails
        GetStats();
    }

    public void OpenAddLogin()
    {
        addLoginPanel.SetActive(true);
    }

    public void OnClickAddLogin()
    {
        var addLoginRequest = new AddUsernamePasswordRequest() {Email = userEmail, Password = userPassword, Username = userName};
        PlayFabClientAPI.AddUsernamePassword(addLoginRequest, OnAddLoginSuccess, OnRegisterFailure);
    }
    
    private void OnAddLoginSuccess(AddUsernamePasswordResult result)
    {
        Debug.Log("Successful added a login!");
        GetStats();    // Load our stats
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        addLoginPanel.SetActive(false);
    }
    #endregion Login

    #region PlayerStats
    // NOTE: All of these scripts will have to communicate with the PlayFab cloud script on developer.playfab.com
    // See the documentation here: https://docs.microsoft.com/en-us/gaming/playfab/features/automation/cloudscript/writing-custom-cloudscript
    
    // NOTE: All of these will change based on the game being made and it's needs
    // COULD move them all to a scriptable object profile

    public int playerLevel;
    public int gameLevel;    // Current level/scene
    public int playerHealth;
    public int playerHighScore;

    // Could be called from PlayFabController.Instance...but we will use a button and hook it up in the inspector
    // Client by default CAN NOT change stats to prevent cheating, so this will need to be done elsewhere
    public void SetStats()
    {
        PlayFabClientAPI.UpdatePlayerStatistics( new UpdatePlayerStatisticsRequest {
                // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
                //Do all stat updates here, might want to use a JSON file or something
                Statistics = new List<StatisticUpdate> 
                {
                    new StatisticUpdate { StatisticName = "PlayerLevel", Value = playerLevel },
                    new StatisticUpdate { StatisticName = "GameLevel", Value = gameLevel },
                    new StatisticUpdate { StatisticName = "PlayerHealth", Value = playerHealth },
                    new StatisticUpdate { StatisticName = "PlayerHighScore", Value = playerHighScore },
                }
            },
            result => { Debug.Log("User statistics updated"); },
            error => { Debug.LogError(error.GenerateErrorReport()); });
    }
    
    public void GetStats()
    {
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            OnGetStats,
            error => Debug.LogError(error.GenerateErrorReport())    // Failed OnGetStats
        );
    }

    public void OnGetStats(GetPlayerStatisticsResult result)
    {
        Debug.Log("Received the following Statistics:");
        foreach (var eachStat in result.Statistics)
        {
            Debug.Log("Statistic (" + eachStat.StatisticName + "): " + eachStat.Value);
            // Using a switch might not be optimal, but we use it currently to verify our stats
            switch (eachStat.StatisticName)
            {
                case "PlayerLevel":
                    playerLevel = eachStat.Value;
                    break;
                case "GameLevel":
                    gameLevel = eachStat.Value;
                    break;
                case "PlayerHealth":
                    playerHealth = eachStat.Value;
                    break;
                case "PlayerHighScore":
                    playerHighScore = eachStat.Value;
                    break;
            }
        }
    }

    // Build the request object and access the API
    public void StartCloudUpdatePlayerStats()
    {
        // This is the call the allows our client to attempt to send our data to be updated by the server
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
        {
            FunctionName = "UpdatePlayerStats", // Arbitrary function name (must exist in your uploaded cloud.js file)
            FunctionParameter = new {currentPlayerLevel = playerLevel, currentGameLevel = gameLevel, currentPlayerHealth = playerHealth, 
                currentPlayerHighScore = playerHighScore}, // The parameter provided to your function
            GeneratePlayStreamEvent = true, // Optional - Shows this event in PlayStream
        }, OnCloudUpdateStats, OnErrorShared);
    }
    // OnCloudHelloWorld defined in the next code block
    
    private static void OnCloudUpdateStats(ExecuteCloudScriptResult result) {
        // CloudScript returns arbitrary results, so you have to evaluate them one step and one parameter at a time
        // NOTE: Old info was outdated, this is the current correct implementation vvvvvvv
        Debug.Log(PlayFab.PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer));   // Getting info from the Cloud  
        JsonObject jsonResult = (JsonObject)result.FunctionResult;
        object messageValue;
        jsonResult.TryGetValue("messageValue", out messageValue); // note how "messageValue" directly corresponds to the JSON values set in CloudScript
        Debug.Log((string)messageValue);
    }

    private static void OnErrorShared(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }

    #endregion PlayerStats
}