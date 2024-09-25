using UnityEngine;
using Fusion;

public class GameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private float _spawnRadius = 3f;

    public void PlayerJoined(PlayerRef playerRef)
    {
        if (!HasStateAuthority) { return; }

        Vector2 randomPositionOffset = Random.insideUnitSphere * _spawnRadius;
        Vector3 spawnPosition = transform.position + 
            new Vector3(randomPositionOffset.x, transform.position.y, randomPositionOffset.y);

        NetworkObject player = Runner.Spawn(_playerPrefab, 
            spawnPosition, Quaternion.identity, playerRef);
        Runner.SetPlayerObject(playerRef, player);
    }

    public void PlayerLeft(PlayerRef playerRef)
    {
        
    }
}
