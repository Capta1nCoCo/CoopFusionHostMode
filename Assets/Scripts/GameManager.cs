using UnityEngine;
using Fusion;

/// <summary>
/// Handles player connections (Spawning of Player instances)
/// </summary>
public class GameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private float _spawnRadius = 3f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }

    public void PlayerJoined(PlayerRef playerRef)
    {
        if (!HasStateAuthority) { return; }

        NetworkObject player = SpawnPlayerObject(playerRef);
        Runner.SetPlayerObject(playerRef, player);
    }

    public void PlayerLeft(PlayerRef playerRef)
    {
        if (!HasInputAuthority) { return; }

        NetworkObject player = Runner.GetPlayerObject(playerRef);
        if (player != null)
        {
            Runner.Despawn(player);
        }
    }

    private NetworkObject SpawnPlayerObject(PlayerRef playerRef)
    {
        Vector3 spawnPosition = ProvideSpawnPosition();
        return Runner.Spawn(_playerPrefab,
                    spawnPosition,
                    Quaternion.identity, 
                    playerRef);
    }

    private Vector3 ProvideSpawnPosition()
    {
        Vector2 randomPositionOffset = Random.insideUnitSphere * _spawnRadius;
        Vector3 spawnPosition = transform.position +
            new Vector3(randomPositionOffset.x, transform.position.y, randomPositionOffset.y);
        return spawnPosition;
    }
}
