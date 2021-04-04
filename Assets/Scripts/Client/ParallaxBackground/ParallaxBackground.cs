using UnityEngine;

namespace Client.ParallaxBackground
{
    public class ParallaxBackground : MonoBehaviour
    {
        public Transform[] backgrounds;
        public bool infiniteHorizontal;
        public bool infiniteVertical;

        private float[] _parallaxScales;
        private float[] _textureUnitSizesX;
        private float[] _textureUnitSizesY;

        private Transform _cameraTransform;
        private Vector3 _lastCameraPosition;

        // Start is called before the first frame update
        private void Start()
        {
            _parallaxScales = new float[backgrounds.Length];
            _textureUnitSizesX = new float[backgrounds.Length];
            _textureUnitSizesY = new float[backgrounds.Length];

            // ReSharper disable once PossibleNullReferenceException
            _cameraTransform = UnityEngine.Camera.main.transform;
            _lastCameraPosition = _cameraTransform.position;

            for (var i = 0; i < backgrounds.Length; ++i)
            {
                var sprite = backgrounds[i].GetComponent<SpriteRenderer>().sprite;
                var texture = sprite.texture;
                _textureUnitSizesX[i] = texture.width / sprite.pixelsPerUnit;
                _textureUnitSizesY[i] = texture.height / sprite.pixelsPerUnit;

                _parallaxScales[i] = backgrounds[i].position.z * -1;
            }
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            var cameraPos = _cameraTransform.position;
            var deltaCameraPos = _lastCameraPosition - cameraPos;

            for (var i = 0; i < backgrounds.Length; ++i)
            {
                var parallaxX = deltaCameraPos.x * _parallaxScales[i];
                var parallaxY = deltaCameraPos.y * _parallaxScales[i];

                backgrounds[i].position = new Vector3(
                    backgrounds[i].position.x + parallaxX,
                    backgrounds[i].position.y + parallaxY,
                    backgrounds[i].position.z);

                if (infiniteHorizontal)
                {
                    if (Mathf.Abs(cameraPos.x - backgrounds[i].position.x) >= _textureUnitSizesX[i])
                    {
                        var offsetPositionX = (cameraPos.x - backgrounds[i].position.x) % _textureUnitSizesX[i];
                        backgrounds[i].position = new Vector3(cameraPos.x + offsetPositionX,
                            backgrounds[i].position.y,
                            backgrounds[i].position.z);
                    }
                }

                if (infiniteVertical)
                {
                    if (Mathf.Abs(cameraPos.y - backgrounds[i].position.y) >= _textureUnitSizesY[i])
                    {
                        var offsetPositionY = (cameraPos.y - backgrounds[i].position.y) % _textureUnitSizesY[i];
                        backgrounds[i].position = new Vector3(backgrounds[i].position.x,
                            cameraPos.y + offsetPositionY,
                            backgrounds[i].position.z);
                    }
                }
            }

            _lastCameraPosition = cameraPos;
        }
    }
}
