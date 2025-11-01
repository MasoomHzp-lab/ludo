using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class ChatManager : MonoBehaviourPunCallbacks
{
[Header("UI")]
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect scroll;

    [Header("Settings")]
    [SerializeField] private int maxLines = 80;      // حداکثر خطوطی که نگه می‌داریم
    [SerializeField] private bool sendOnEnter = true;
    [SerializeField] private string systemTag = "<color=#999999>[System]</color>";

    private readonly Queue<string> lines = new Queue<string>();
    private PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999); // اسم پیش‌فرض
    }

    void Start()
    {
        AddLine($"{systemTag} Chat ready. Your name: <b>{PhotonNetwork.NickName}</b>");
    }

    void Update()
    {
        if (sendOnEnter && input && input.isFocused && Input.GetKeyDown(KeyCode.Return))
            OnClickSend();
    }

    // دکمه Send → این را وصل کن
    public void OnClickSend()
    {
        if (!input) return;
        string msg = (input.text ?? "").Trim();
        if (string.IsNullOrEmpty(msg)) return;

        // محدودیت خیلی ساده برای اسپم:
        if (msg.Length > 200) msg = msg.Substring(0, 200);

        pv.RPC(nameof(RPC_Chat), RpcTarget.All, PhotonNetwork.NickName, msg);
        input.text = "";
        input.ActivateInputField();
    }

    [PunRPC]
    void RPC_Chat(string sender, string message)
    {
        AddLine($"<b>{Escape(sender)}</b>: {Escape(message)}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddLine($"{systemTag} {Escape(newPlayer.NickName)} joined.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        AddLine($"{systemTag} {Escape(otherPlayer.NickName)} left.");
    }

    // ---------- helpers ----------
    void AddLine(string line)
    {
        lines.Enqueue(line);
        while (lines.Count > maxLines) lines.Dequeue();

        if (logText)
        {
            logText.text = string.Join("\n", lines);
            // اسکرول به پایین
            if (scroll)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 0f;
            }
        }
    }

    // ساده‌سازی برای جلوگیری از خراب شدن rich text
    string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
