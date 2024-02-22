using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO;
using System.Net;
using System.Collections;

public class AutoNgrokService : MonoBehaviour
{

    [SerializeField] private bool restartTunnelButton;

    [SerializeField] private bool logAllOuputs;
    [SerializeField] private bool killTunnelsButton;

    private Process _ngrokProcess;
    private string _pathToNgrokExe;
    //public string TunnelURL = "";
    //private bool _tunnelURLFound = false;

    private void Start()
    {
        _pathToNgrokExe = Path.Combine(Application.streamingAssetsPath, AppConfig.inst.GetS("PATH_TO_NGROK_EXE"));

        Debug.Log($"Path to ngrok: {_pathToNgrokExe}"); 
        if (!File.Exists(_pathToNgrokExe))
        {
            Debug.Log("Path to ngrok.exe does not exist"); 
            return;
        }

        StartCoroutine(RestartNgrokTunnel());

    }


    private IEnumerator RestartNgrokTunnel()
    {
        Debug.Log("Restarting Ngrok Tunnel"); 
        KillAllNgrokProcesses();

        yield return new WaitForSeconds(1);

        StartNgrokTunnel();
    }

    private void OnValidate()
    {
        if (restartTunnelButton)
        {
            restartTunnelButton = false;
            StartCoroutine(RestartNgrokTunnel());
        }
        if (killTunnelsButton)
        {
            killTunnelsButton = false;
            KillAllNgrokProcesses();
        }
    }

    public void StartNgrokTunnel()
    {

        string localPort = AppConfig.inst.GetI("localHostPort").ToString(); 

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = _pathToNgrokExe;
        psi.WorkingDirectory = Path.GetDirectoryName(_pathToNgrokExe);
        psi.Arguments = $"http --domain={Secrets.TUNNEL_DOMAIN} {localPort}";

        psi.UseShellExecute = false;
        psi.RedirectStandardInput = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.CreateNoWindow = true;

        _ngrokProcess = Process.Start(psi);

        _ngrokProcess.OutputDataReceived += HandleNgrokOutput;
        _ngrokProcess.ErrorDataReceived += HandleNgrokErrorOutput;

        _ngrokProcess.BeginOutputReadLine();
        _ngrokProcess.BeginErrorReadLine();

        Debug.Log($"Done starting tunnel with arguments: http --domain={Secrets.TUNNEL_DOMAIN} {localPort}");

    }

    private void HandleNgrokOutput(object sender, DataReceivedEventArgs e)
    {

        if (logAllOuputs)
            Debug.Log("message from tunnel: " + e.Data);

    }

    private void HandleNgrokErrorOutput(object sender, DataReceivedEventArgs e)
    {
        string msg = e.Data;
        Debug.Log(msg);

    }


    private void OnDestroy()
    {
        KillAllNgrokProcesses();
    }

    public void KillAllNgrokProcesses()
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "taskkill";

        psi.Arguments = $"/f /im ngrok.exe";

        psi.UseShellExecute = false;
        psi.RedirectStandardInput = false;
        psi.RedirectStandardOutput = false;
        psi.RedirectStandardError = false;
        psi.CreateNoWindow = true;

        Process.Start(psi);

        Debug.Log("Killed all ngrok processes");

        if (_ngrokProcess == null || _ngrokProcess.HasExited)
            return;

        _ngrokProcess.CancelOutputRead();
        _ngrokProcess.Kill();
        _ngrokProcess.Dispose();
        _ngrokProcess = null;



    }


}