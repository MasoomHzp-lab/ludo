using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class NetGameManager : MonoBehaviourPunCallbacks
{
     [Header("Refs")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Dice dice;
    [SerializeField] private BoardManager boardManager;

    [Header("Players (optional for enable/disable)")]
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private GameObject player3;
    [SerializeField] private GameObject player4;

    private int currentTurnIndex = 0;   // 0..N-1
    private int playerCount = 4;

    void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("Not in Photon room.");
            return;
        }

        // تعداد بازیکن از CustomProperties اتاق (اگر نبود، پیش‌فرض 4)
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("maxP", out object maxP))
        {
            playerCount = (int)(byte)maxP;
        }

        // اسلات‌های اضافه رو خاموش کن (اختیاری)
        ApplyPlayerSlots(playerCount);

        // فقط Master بازی را استارت می‌کند
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_StartGame), RpcTarget.AllBuffered, playerCount);
    }

    void ApplyPlayerSlots(int count)
    {
        if (player1) player1.SetActive(count >= 1);
        if (player2) player2.SetActive(count >= 2);
        if (player3) player3.SetActive(count >= 3);
        if (player4) player4.SetActive(count >= 4);

        // اگر در GameManager متدی با این اسم داشته باشی، فراخوانی می‌شود (اختیاری)
        if (gameManager) gameManager.SendMessage("OnStartMatch", count, SendMessageOptions.DontRequireReceiver);
    }

    [PunRPC]
    void RPC_StartGame(int pCount)
    {
        playerCount = pCount;
        currentTurnIndex = 0;

        // اگر خواستی، در GameManager قلاب بگذار: void OnMatchStarted(int players)
        if (gameManager) gameManager.SendMessage("OnMatchStarted", playerCount, SendMessageOptions.DontRequireReceiver);

        if (PhotonNetwork.IsMasterClient)
            Invoke(nameof(DoTurnTick), 0.1f);
    }

    void DoTurnTick()
    {
        // Master تاس را «تعیین» می‌کند (عدد 1..6) و به همه اعلام می‌کند.
        // اگر Dice شما متدی به نام RollServer داشت، از آن استفاده کن؛ وگرنه Random:
        int diceVal = Random.Range(1, 7);
        photonView.RPC(nameof(RPC_OnDice), RpcTarget.All, currentTurnIndex, diceVal);
    }

    [PunRPC]
    void RPC_OnDice(int seat, int val)
    {
        // نمایش انیمیشن/صورت تاس (اختیاری)
        if (dice) dice.SendMessage("PlayRollAnim", val, SendMessageOptions.DontRequireReceiver);

        // اگر در GameManager قلاب ساختی، بهت خبر می‌دهد (اختیاری)
        if (gameManager)
        {
            // امضاهای اختیاری که می‌تونی بسازی:
            // void OnNetworkDice(int seat, int value)
            // یا void OnDiceRolled(int seat, int value)
            gameManager.SendMessage("OnNetworkDice", new object[] { seat, val }, SendMessageOptions.DontRequireReceiver);
            gameManager.SendMessage("OnDiceRolled", new object[] { seat, val }, SendMessageOptions.DontRequireReceiver);
        }

        // حالا بازیکنِ نوبتی باید مهره انتخاب کند.
        // از UI/PlayerController وقتی کلیک شد: FindObjectOfType<NetGameManager>().SendChosenToken(seat, tokenIndex)
    }

    // از UI صدا بزن: وقتی بازیکن مهره را انتخاب کرد
    public void SendChosenToken(int seat, int tokenIndex)
    {
        photonView.RPC(nameof(RPC_CommitMove), RpcTarget.MasterClient, seat, tokenIndex);
    }

    [PunRPC]
    void RPC_CommitMove(int seat, int tokenIndex, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (seat != currentTurnIndex) return;

        // اگر GameManager متد IsLegalMove دارد، بررسی کن
        bool legal = true;
        if (gameManager)
        {
            var mi = gameManager.GetType().GetMethod("IsLegalMove");
            if (mi != null)
            {
                object res = mi.Invoke(gameManager, new object[] { seat, tokenIndex });
                if (res is bool b) legal = b;
            }
        }
        if (!legal) return;

        // مقصد حرکت:
        int destSquare = -1;

        // اگر GameManager متد ResolveMove دارد، از آن بگیر
        if (gameManager)
        {
            var mi = gameManager.GetType().GetMethod("ResolveMove");
            if (mi != null)
            {
                object res = mi.Invoke(gameManager, new object[] { seat, tokenIndex });
                if (res is int d) destSquare = d;
            }
        }

        // اگر چیزی نداشتی، بگذار Apply طرف مقابل خودش حساب کند (destSquare = -1)
        photonView.RPC(nameof(RPC_ApplyMove), RpcTarget.All, seat, tokenIndex, destSquare);
    }

    [PunRPC]
    void RPC_ApplyMove(int seat, int tokenIndex, int destSquare)
    {
        // اگر GameManager متد ApplyMove دارد، فراخوانی کن
        bool applied = false;
        if (gameManager)
        {
            var mi = gameManager.GetType().GetMethod("ApplyMove");
            if (mi != null)
            {
                mi.Invoke(gameManager, new object[] { seat, tokenIndex, destSquare });
                applied = true;
            }
            else
            {
                // یا اگر متدی مثل MoveToken(int seat,int token,int diceVal) داشتی می‌تونی از SendMessage استفاده کنی
                gameManager.SendMessage("MoveToken", new object[] { seat, tokenIndex, destSquare }, SendMessageOptions.DontRequireReceiver);
            }
        }

        // پایان نوبت → نوبت بعد
        if (PhotonNetwork.IsMasterClient)
        {
            currentTurnIndex = (currentTurnIndex + 1) % playerCount;
            Invoke(nameof(DoTurnTick), 0.3f);
        }
    }
}
