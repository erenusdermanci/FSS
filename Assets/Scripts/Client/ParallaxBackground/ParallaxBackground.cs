﻿using UnityEngine;

namespace Client.ParallaxBackground
{
    public class ParallaxBackground : MonoBehaviour
    {
        public Transform[] backgrounds;
        public bool infiniteHorizontal;
        public bool infiniteVertical;

        private float[] parallaxScales;
        private float[] textureUnitSizesX;
        private float[] textureUnitSizesY;

        private Transform cameraTransform;
        private Vector3 lastCameraPosition;

        // Start is called before the first frame update
        private void Start()
        {
            parallaxScales = new float[backgrounds.Length];
            textureUnitSizesX = new float[backgrounds.Length];
            textureUnitSizesY = new float[backgrounds.Length];

            // ReSharper disable once PossibleNullReferenceException
            cameraTransform = UnityEngine.Camera.main.transform;
            lastCameraPosition = cameraTransform.position;

            for (var i = 0; i < backgrounds.Length; ++i)
            {
                var sprite = backgrounds[i].GetComponent<SpriteRenderer>().sprite;
                var texture = sprite.texture;
                textureUnitSizesX[i] = texture.width / sprite.pixelsPerUnit;
                textureUnitSizesY[i] = texture.height / sprite.pixelsPerUnit;

                parallaxScales[i] = backgrounds[i].position.z * -1;
            }
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            var cameraPos = cameraTransform.position;
            var deltaCameraPos = lastCameraPosition - cameraPos;

            for (var i = 0; i < backgrounds.Length; ++i)
            {
                var parallaxX = deltaCameraPos.x * parallaxScales[i];
                var parallaxY = deltaCameraPos.y * parallaxScales[i];

                backgrounds[i].position = new Vector3(
                    backgrounds[i].position.x + parallaxX,
                    backgrounds[i].position.y + parallaxY,
                    backgrounds[i].position.z);

                if (infiniteHorizontal)
                {
                    if (Mathf.Abs(cameraPos.x - backgrounds[i].position.x) >= textureUnitSizesX[i])
                    {
                        var offsetPositionX = (cameraPos.x - backgrounds[i].position.x) % textureUnitSizesX[i];
                        backgrounds[i].position = new Vector3(cameraPos.x + offsetPositionX,
                            backgrounds[i].position.y,
                            backgrounds[i].position.z);
                    }
                }

                if (infiniteVertical)
                {
                    if (Mathf.Abs(cameraPos.y - backgrounds[i].position.y) >= textureUnitSizesY[i])
                    {
                        var offsetPositionY = (cameraPos.y - backgrounds[i].position.y) % textureUnitSizesY[i];
                        backgrounds[i].position = new Vector3(backgrounds[i].position.x,
                            cameraPos.y + offsetPositionY,
                            backgrounds[i].position.z);
                    }
                }
            }

            lastCameraPosition = cameraPos;
        }
    }
}
