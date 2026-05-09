using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class BoardFactory
{
    private readonly ObjectPool _pool;
    private readonly Transform _container;
    private readonly Sprite[] _sprites;
    private readonly HashSet<TileView> _activeTiles;

    public BoardFactory(ObjectPool pool, Transform container, Sprite[] sprites, HashSet<TileView> activeTiles)
    {
        _pool = pool;
        _container = container;
        _sprites = sprites;
        _activeTiles = activeTiles;
    }

    public async UniTask GenerateBoardAsync(LevelData levelData, System.Action<TileView> onTileClicked)
    {
        var generator = new SolvableGenerator();
        var tilesData = generator.Generate(levelData);
        var instances = new List<TileView>();
        var groupedByLayer = tilesData.GroupBy(t => t.Layer).OrderBy(g => g.Key).ToList();

        foreach (var layerGroup in groupedByLayer)
        {
            var spawnTasks = new List<UniTask>();

            foreach (var data in layerGroup)
            {
                var tile = _pool.Get<TileView>(GameConstants.POOL_KEY_TILE);
                tile.transform.SetPositionAndRotation(data.Position, Quaternion.identity);
                tile.transform.SetParent(_container);

                var sprite = _sprites.First(s => s.name == data.Id);
                tile.Setup(data.Id, data.Layer, sprite, t => 
                {
                    _pool.Return(t.gameObject);
                    _pool.Get<ParticleReturnPool>(GameConstants.POOL_KEY_MATCH_EFFECT,t.transform.position,Quaternion.identity);

                });
                tile.OnClicked += onTileClicked;
                
                instances.Add(tile);
                _activeTiles.Add(tile);
                spawnTasks.Add(tile.SpawnPopupAnimationAsync());
            }

            await UniTask.WhenAll(spawnTasks);
            await UniTask.Delay(100);
        }

        BuildDependencies(instances);
    }

    private void BuildDependencies(IReadOnlyList<TileView> tiles)
    {
        int count = tiles.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                var a = tiles[i];
                var b = tiles[j];

                if (a.Layer == b.Layer || !IsOverlapping(a, b)) continue;

                if (a.Layer < b.Layer) a.AddBlocker(b);
                else b.AddBlocker(a);
            }
        }
    }

    private bool IsOverlapping(TileView a, TileView b)
    {
        return a.transform.position.x < b.transform.position.x + b.Size &&
               a.transform.position.x + a.Size > b.transform.position.x &&
               a.transform.position.y < b.transform.position.y + b.Size &&
               a.transform.position.y + a.Size > b.transform.position.y;
    }
}