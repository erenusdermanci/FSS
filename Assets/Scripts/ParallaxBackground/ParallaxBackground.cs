using UnityEngine;

namespace ParallaxBackground
{
    public class ParallaxBackground : MonoBehaviour
    {
        public Transform[] backgrounds;
        private float[] parallaxScales;
        private float[] textureUnitSizesX;
        // private float[] textureUnitSizesY;
        // public float smoothing;

        [SerializeField] private bool infiniteHorizontal;
        // [SerializeField] private bool infiniteVertical;

        private Transform cameraTransform;
        private Vector3 lastCameraPosition;

        // Start is called before the first frame update
        private void Start()
        {
            parallaxScales = new float[backgrounds.Length];
            textureUnitSizesX = new float[backgrounds.Length];
            // textureUnitSizesY = new float[backgrounds.Length];

            cameraTransform = Camera.main.transform;
            lastCameraPosition = cameraTransform.position;

            for (var i = 0; i < backgrounds.Length; ++i)
            {
                var sprite = backgrounds[i].GetComponent<SpriteRenderer>().sprite;
                var texture = sprite.texture;
                textureUnitSizesX[i] = texture.width / sprite.pixelsPerUnit;
                // textureUnitSizesY[i] = texture.height / sprite.pixelsPerUnit;

                parallaxScales[i] = backgrounds[i].position.z * -1;
            }
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            var cameraPos = cameraTransform.position;
            var deltaMovement = cameraPos - lastCameraPosition;

            for (var i = 0; i < backgrounds.Length; ++i)
            {
                var parallax = (lastCameraPosition.x - cameraPos.x) * parallaxScales[i];
                // var parallaxed = new Vector3(deltaMovement.x * parallaxScales[i],
                //     deltaMovement.y * parallaxScales[i],
                //     deltaMovement.z);

                //var lerped = Vector3.Lerp(backgrounds[i].position, parallaxed, smoothing * Time.deltaTime);
                backgrounds[i].position = new Vector3(
                    backgrounds[i].position.x + parallax,
                    backgrounds[i].position.y,
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

                // if (infiniteVertical)
                // {
                //     if (Mathf.Abs(cameraPos.y - backgrounds[i].position.y) >= textureUnitSizesY[i])
                //     {
                //         var offsetPositionY = (cameraPos.y - backgrounds[i].position.y) % textureUnitSizesY[i];
                //         backgrounds[i].position = new Vector3(backgrounds[i].position.x,
                //             cameraPos.y + offsetPositionY,
                //             backgrounds[i].position.z);
                //     }
                // }
            }

            lastCameraPosition = cameraPos;
        }
    }
}
