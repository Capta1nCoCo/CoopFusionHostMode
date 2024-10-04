using Fusion;
using UnityEngine;

public class PlayerNickname : NetworkBehaviour
{
    [SerializeField] private UINameplate _nameplate;

    [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
    public string Nickname { get; private set; }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            RPC_SetNickname(PlayerPrefs.GetString(Constants.PlayerPrefsVars.PLAYER_NAME));
        }
        OnNicknameChanged();
    }

    private void OnNicknameChanged()
    {
        if (HasInputAuthority) { return; }
        _nameplate.SetNickname(Nickname);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetNickname(string nickname)
    {
        Nickname = nickname;
    }
}