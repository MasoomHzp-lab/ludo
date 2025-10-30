using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class OnlinePageController : MonoBehaviourPunCallbacks
{
 [Header("Scenes")]
    [SerializeField] private string gameScene = "MainLand_Online";

    [Header("Selection")]
    [SerializeField, Range(2,4)] private int chosenPlayers = 4;
    [SerializeField] private TMPro.TMP_InputField roomCodeInput; // اختیاری، اگر داری
    private string roomCode = "";

    private enum PendingAction { None, Create, Join }
    private PendingAction pending = PendingAction.None;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        // اتصال ملایم: اگر قبلاً وصل نیستیم
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
    }

    // ===== UI =====
    public void SetPlayers2() => chosenPlayers = 2;
    public void SetPlayers3() => chosenPlayers = 3;
    public void SetPlayers4() => chosenPlayers = 4;
    public void OnRoomCodeChanged(string s) => roomCode = (s ?? "").Trim().ToUpper();

    // یک دکمه‌ی «Start» هوشمند: بدون کد → ساخت روم، با کد → پیوستن
    public void StartOnlineGame()
    {
        if (roomCodeInput) OnRoomCodeChanged(roomCodeInput.text);
        if (string.IsNullOrEmpty(roomCode)) RequestCreate();
        else RequestJoin();
    }

    public void RequestCreate()
    {
        pending = PendingAction.Create;
        EnsureConnectedThenProceed();
    }

    public void RequestJoin()
    {
        pending = PendingAction.Join;
        EnsureConnectedThenProceed();
    }

    // ===== اتصال مرحله‌ای =====
    private void EnsureConnectedThenProceed()
    {
        // اگر هنوز به مستر وصل نیستیم، وصل شو؛ ادامه کار در کال‌بک‌ها انجام می‌شود
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        // اگر به لابی وصل نیستیم، برو لابی
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
            return;
        }

        // آماده‌ایم → اقدام معلق را انجام بده
        DoPendingAction();
    }

    private void DoPendingAction()
    {
        if (pending == PendingAction.Create)
        {
            string code = GenerateCode(6);
            var opt = new RoomOptions { MaxPlayers = (byte)chosenPlayers };
            bool ok = PhotonNetwork.CreateRoom(code, opt, TypedLobby.Default);
            if (!ok) Debug.LogWarning("CreateRoom call returned false (will get callback with reason).");
            pending = PendingAction.None;
        }
        else if (pending == PendingAction.Join)
        {
            if (string.IsNullOrEmpty(roomCode)) { Debug.LogWarning("No room code to join."); return; }
            bool ok = PhotonNetwork.JoinRoom(roomCode);
            if (!ok) Debug.LogWarning("JoinRoom call returned false (will get callback with reason).");
            pending = PendingAction.None;
        }
    }

    // ===== Callbacks =====
    public override void OnConnectedToMaster()
    {
        // بعد از اتصال به مستر، حتماً وارد لابی شو
        if (!PhotonNetwork.InLobby) PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        // حالا می‌تونیم اقدام معلق را انجام بدهیم
        DoPendingAction();
    }

    public override void OnCreatedRoom()
    {
        var ht = new ExitGames.Client.Photon.Hashtable { { "maxP", chosenPlayers } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        // می‌تونی کد روم رو به UI نشون بدی: PhotonNetwork.CurrentRoom.Name
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(gameScene);
    }

    public override void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogError($"CreateRoom failed: {code} - {msg}");
        // می‌تونی دوباره تلاش کنی یا کد جدید بسازی
    }

    public override void OnJoinRoomFailed(short code, string msg)
    {
        Debug.LogError($"JoinRoom failed: {code} - {msg}");
        // پیام مناسب به کاربر بده (کد اتاق اشتباه یا اتاق پر)
    }

    private string GenerateCode(int len)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var sb = new System.Text.StringBuilder(len);
        var rng = new System.Random();
        for (int i = 0; i < len; i++) sb.Append(chars[rng.Next(chars.Length)]);
        return sb.ToString();
    }
}
