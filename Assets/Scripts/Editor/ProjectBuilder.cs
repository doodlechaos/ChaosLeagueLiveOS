using System.IO;
using UnityEditor;
using UnityEngine;

public class ProjectBuilder : EditorWindow
{
    private string PublicBuildPath = "E:/UnityBuilds/PublicBuilds";
    private string PrivateBuildPath = "E:/UnityBuilds/PrivateBuilds";

    private string ProjectName = "defaultProjName"; 

    [MenuItem("Tools/Project Builder")]
    public static void ShowWindow()
    {
        GetWindow<ProjectBuilder>("Project Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Build Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        PublicBuildPath = EditorGUILayout.TextField("Public Build Path", PublicBuildPath);
        if (GUILayout.Button("Browse", GUILayout.Width(100)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Build Folder", PublicBuildPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                PublicBuildPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        PrivateBuildPath = EditorGUILayout.TextField("Private Build Path", PrivateBuildPath);
        if (GUILayout.Button("Browse", GUILayout.Width(100)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Build Folder", PrivateBuildPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                PrivateBuildPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        ProjectName = EditorGUILayout.TextField("Build Name", ProjectName);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Build Private"))
        {
            BuildProject();
        }

        if (GUILayout.Button("Build Public"))
        {
            BuildProject(isPublic: true);
        }

        if (GUILayout.Button("view curr defaults"))
        {
            // Log some of the current build options retrieved from the Build Settings Window
            BuildPlayerOptions buildPlayerOptions = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
            Debug.Log("BuildPlayerOptions\n"
                + "Scenes: " + string.Join(",", buildPlayerOptions.scenes) + "\n"
                + "Build location: " + buildPlayerOptions.locationPathName + "\n"
                + "Options: " + buildPlayerOptions.options + "\n"
                + "Target: " + buildPlayerOptions.target);
        }
    }

    private void BuildProject(bool isPublic = false)
    {
        string buildFolderPath = Path.Combine(isPublic ? PublicBuildPath : PrivateBuildPath, ProjectName);

        // Set options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MainScene.unity"}; // Add your scenes here
        buildPlayerOptions.locationPathName = Path.Combine(buildFolderPath, $"{ProjectName}.exe"); // Change to your game name
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64; // Change this to your desired platform
        if(!isPublic)
            buildPlayerOptions.options = BuildOptions.Development;

        // Perform build
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log("Build completed: " + buildFolderPath);

        string pathToBuiltConfigFile = Path.Combine(buildFolderPath, $"{ProjectName}_Data", "StreamingAssets", "config.json");
        AppConfig.LoadFromJson(File.ReadAllText(pathToBuiltConfigFile));
        string pathToEnv = Path.Combine(Application.streamingAssetsPath, "secrets.env");
        AppConfig.LoadEnvironmentVariables(pathToEnv);


        if (isPublic)
        {
            //Delete the AWS public and private key, disable Polly for TTS and use local
            AppConfig.inst.SetV("AWS_ACCESS_KEY", "");
            AppConfig.inst.SetV("AWS_SECRET_KEY", "");

            //Delete the LocalPC key
            AppConfig.inst.SetV("LOCAL_PC_KEY", "");

            //Delete the spotifyClient ID and secret
            AppConfig.inst.SetV("SpotifyClientID", "");
            AppConfig.inst.SetV("SpotifyClientSecret", "");

            //Delete the Load balancer URL
            AppConfig.inst.SetV("LoadBalancerURL", "");

            //Delete the webhook urls and don't send them
            AppConfig.inst.SetV("ERROR_WEBHOOK_URL", "");
            AppConfig.inst.SetV("DONATION_WEBHOOK_URL", "");
            AppConfig.inst.SetV("Send_Error_Webhooks", false);
            AppConfig.inst.SetV("Send_Donation_Webhooks", false);

            //Disable invites
            AppConfig.inst.SetV("EnablePyramidSchemeInvites", false);

            AppConfig.inst.SetV("API_MODE", "PUBLIC");

            //Optional, remove client ids 
            AppConfig.inst.SetV("CLIENT_ID_PRIVATE", "");
            AppConfig.inst.SetV("CLIENT_ID_STAGING", "");

            //Delete Client Secret
            AppConfig.inst.SetV("CLIENT_SECRET_PRIVATE", "");
            AppConfig.inst.SetV("CLIENT_SECRET_STAGING", "");

            //Delete the database backups folder and database
            string backupsFolderPath = Path.Combine(buildFolderPath, $"{ProjectName}_Data", "StreamingAssets", "DatabaseBackups");
            // Delete the backups folder if it exists
            if (Directory.Exists(backupsFolderPath))
            {
                Directory.Delete(backupsFolderPath, true);
                Debug.Log("Deleted db backups folder");
            }

            string dbPath = Path.Combine(buildFolderPath, $"{ProjectName}_Data", "StreamingAssets", "PlayerProfiles.db");
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                Debug.Log("Deleted db file");
            }

            //DELTE THE ENVIRONMENT VARIABLES
            string pathToEnvInBuild = Path.Combine(buildFolderPath, $"{ProjectName}_Data", "StreamingAssets", "secrets.env");
            if (File.Exists(pathToEnvInBuild))
            {
                File.Delete(pathToEnvInBuild);
                Debug.Log("Deleted secrets.env file");
            }
            else
                Debug.LogError("FAILED TO DELETE SECRETS.ENV"); 

        }
        else
        {
            AppConfig.inst.SetV("API_MODE", "PRIVATE");

        }

        AppConfig.SaveConfigFile(pathToBuiltConfigFile);
        Debug.Log("Publicised config file");

    }


}