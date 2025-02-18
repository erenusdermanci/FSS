﻿using System;
using Entities;
using UnityEngine;
using Utils;
using Utils.Drawing;
using Color = UnityEngine.Color;

namespace Tools.BlockMapper
{
    public class BlockMapper : MonoBehaviour
    {
        [HideInInspector]
        public DrawingParameters.DrawingParameters parameters;

        public GameObject entityToInstanciate;
        private Entity _entity;

        private Vector2i? _lastPointDrawn;
        private Vector2i? _lastPointDrawnForLine;
        private Vector2 _mouseWorldPosition;

        private void Awake()
        {
            parameters = GetComponent<DrawingParameters.DrawingParameters>();

            if (entityToInstanciate == null)
                throw new InvalidOperationException("You must specify an entity to instantiate!");

            var entityObject = Instantiate(entityToInstanciate, new Vector3(0, 0, 0), Quaternion.identity);
            _entity = entityObject.GetComponent<Entity>();
            _entity.enableBlockMap = true;
        }

        private void Update()
        {
            var mainCamera = Camera.main;
            if (mainCamera is null)
                return;
            _mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            DrawBounds();

            var sprite = _entity.spriteRenderer.sprite;
            var blockPosition = new Vector2i(
                (int) (Mathf.Floor(_mouseWorldPosition.x / _entity.texelSize) + sprite.texture.width / 2f),
                (int) (Mathf.Floor(_mouseWorldPosition.y / _entity.texelSize) + sprite.texture.height / 2f)
            );
            if (blockPosition.x < 0 || blockPosition.y < 0 || blockPosition.x >= sprite.texture.width || blockPosition.y >= sprite.texture.height)
                return;
            switch (parameters.tool)
            {
                case DrawingToolType.Brush:
                    UpdateBrush(blockPosition);
                    var texelX = Mathf.Floor(_mouseWorldPosition.x * sprite.pixelsPerUnit);
                    var texelY = Mathf.Floor(_mouseWorldPosition.y * sprite.pixelsPerUnit);
                    parameters.DrawBrush(texelX, texelY, _entity.texelSize);
                    break;
                case DrawingToolType.Fill:
                    UpdateFill(blockPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _entity.ReloadBlockMap();
        }

        private void UpdateBrush(Vector2i blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_lastPointDrawnForLine != null && Input.GetKey(KeyCode.LeftShift))
                    Draw.Line(_lastPointDrawnForLine.Value.x, _lastPointDrawnForLine.Value.y,
                        blockPosition.x, blockPosition.y, DrawBlocks);
            }
            else if (Input.GetMouseButton(0))
            {
                if (_lastPointDrawn == null)
                    DrawBlocks(blockPosition.x, blockPosition.y);
                else
                    Draw.Line(_lastPointDrawn.Value.x, _lastPointDrawn.Value.y, blockPosition.x, blockPosition.y, DrawBlocks);
                _lastPointDrawn = new Vector2i(blockPosition.x, blockPosition.y);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _lastPointDrawnForLine = _lastPointDrawn;
                _lastPointDrawn = null;
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                if (_lastPointDrawnForLine != null)
                {
                    DrawDebugLine(_lastPointDrawnForLine.Value, blockPosition, Color.white);
                }
            }
        }

        private void DrawDebugLine(Vector2i start, Vector2i end, Color32 color)
        {
            var sprite = _entity.spriteRenderer.sprite;
            var xStart = (start.x - sprite.texture.width / 2f) * _entity.texelSize + _entity.texelSize / 2f;
            var yStart = (start.y - sprite.texture.height / 2f) * _entity.texelSize + _entity.texelSize / 2f;
            var xEnd = (end.x - sprite.texture.width / 2f) * _entity.texelSize + _entity.texelSize / 2f;
            var yEnd = (end.y - sprite.texture.height / 2f) * _entity.texelSize + _entity.texelSize / 2f;
            Debug.DrawLine(new Vector2(xStart, yStart), new Vector2(xEnd, yEnd), color);
        }

        private void UpdateFill(Vector2i blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Draw.Fill(blockPosition.x, blockPosition.y, _entity.GetBlockType, (i, j) => _entity.PutBlockType(i, j, parameters.block));
            }
        }

        private void DrawBounds()
        {
            var sprite = _entity.spriteRenderer.sprite;
            var textureWorldWidth = sprite.texture.width * _entity.texelSize;
            var textureWorldHeight = sprite.texture.height * _entity.texelSize;
            var x = -textureWorldWidth / 2.0f;
            var y = -textureWorldHeight / 2.0f;
            Debug.DrawLine(new Vector2(x, y), new Vector2(x + textureWorldWidth, y));
            Debug.DrawLine(new Vector2(x, y + textureWorldHeight), new Vector2(x, y));
            Debug.DrawLine(new Vector2(x + textureWorldWidth, y), new Vector2(x + textureWorldWidth, y + textureWorldHeight));
            Debug.DrawLine(new Vector2(x, y + textureWorldHeight), new Vector2(x + textureWorldWidth, y + textureWorldHeight));
        }

        private void DrawBlocks(int x, int y)
        {
            switch (parameters.brush)
            {
                case DrawingBrushType.Box:
                    Draw.Rectangle(x, y, parameters.size, parameters.size, (i, j) => _entity.PutBlockType(i, j, parameters.block));
                    break;
                case DrawingBrushType.Circle:
                    Draw.Circle(x, y, parameters.size, (i, j) => _entity.PutBlockType(i, j, parameters.block));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
