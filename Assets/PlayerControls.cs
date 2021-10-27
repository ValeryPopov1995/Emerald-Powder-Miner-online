using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerControls : MonoBehaviour, IPunObservable
{
    public Vector2Int Position;

    [SerializeField] Transform ladder;
    [SerializeField] float moveCulldown = .9f;

    Vector2Int lastFramePosition;
    PhotonView view;
    MapController map;
    SpriteRenderer render;
    double lastTick;
    bool isRight = false;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) stream.SendNext(isRight);
        else if (stream.IsReading) isRight = (bool)stream.ReceiveNext();
    }

    private void Start()
    {
        view = GetComponent<PhotonView>();
        map = FindObjectOfType<MapController>();
        render = GetComponent<SpriteRenderer>();

        Position = new Vector2Int((int)transform.position.x, (int)transform.position.y);
        FindObjectOfType<MapController>().AddPlayer(this);

        setLadderLength(0);

        if (!view.IsMine) render.color = Color.red; // enemy
    }

    private void Update()
    {
        if (view.IsMine && map.isHavePlayerBelow(this) && PhotonNetwork.NetworkClientState != ClientState.Leaving) die();

        if (isRight) render.flipX = false;
        else render.flipX = true;

        Position = new Vector2Int((int)transform.position.x, (int)transform.position.y);
        
        if (lastFramePosition != Position)
        {
            setLadderLength(map.MesureLadderLength(Position));
            lastFramePosition = Position;
        }

        if (!view.IsMine) return;
        if (PhotonNetwork.Time < lastTick + moveCulldown) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount != 2) return;

        var beforMovePos = Position;

        #region input and clamp pos
        if (Input.GetKeyDown(KeyCode.W))
            Position += Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.A))
        {
            Position += Vector2Int.left;
            isRight = false;
        }
        if (Input.GetKeyDown(KeyCode.S))
            Position += Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.D))
        {
            Position += Vector2Int.right;
            isRight = true;
        }

        if (Position.x < 0) Position.x = 0;
        if (Position.x > 19) Position.x = 19;
        if (Position.y < 0) Position.y = 0;
        if (Position.y > 9) Position.y = 9;
        #endregion

        if (Position == beforMovePos) return;
        transform.position = new Vector3(Position.x, Position.y, 0);

        lastTick = PhotonNetwork.Time;
        PhotonNetwork.RaiseEvent(42,
            Position,
            new RaiseEventOptions() { Receivers = ReceiverGroup.All },
            new SendOptions() { Reliability = true });
    }

    void setLadderLength(int length)
    {
        for (int i = 0; i < ladder.childCount; i++)
            ladder.GetChild(i).gameObject.SetActive(i < length);

        while (ladder.childCount < length)
            Instantiate(ladder.GetChild(0), ladder.position + Vector3.down * (ladder.childCount +1), Quaternion.identity, ladder);
    }

    void die()
    {
        Debug.Log("Death");
        PhotonNetwork.LeaveRoom();
    }
}