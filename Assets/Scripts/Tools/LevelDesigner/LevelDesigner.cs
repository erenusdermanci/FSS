using System.Collections.Generic;
using Assets;
using UnityEngine;
using Utils;

namespace Tools.LevelDesigner
{
    public class LevelDesigner : MonoBehaviour
    {
        private List<Asset> _assets;

        public void AssetAwake(Asset asset)
        {
            _assets.Add(asset);

            // assign this asset Unique Id that will be transmitted to blocks when they are put in the grid
            asset.id = UniqueIdGenerator.Next();

            // add temporary collider on the asset, for mouse selection
            var tmpCollider = asset.gameObject.AddComponent<BoxCollider2D>();
            tmpCollider.tag = "LevelDesigner";
            tmpCollider.enabled = true;

            // move the asset at the camera position
            var cameraPosition = Camera.main.transform.position;
            asset.transform.position = new Vector2(cameraPosition.x, cameraPosition.y);
        }
    }
}