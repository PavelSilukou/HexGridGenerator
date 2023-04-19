using System;
using System.Collections.Generic;
using HexGridGenerator.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HexGridGenerator
{
    public class HexGridGenerator : EditorWindow
    {
        private float _radius = 5.0f;
        private float _convertedRadius = 5.0f;
        private int _selectedRadiusType = 0;
        private readonly string[] _radiusTypes = { "Outer", "Inner" };
        private int _selectedOrientation = 0;
        private readonly string[] _orientations = { "Flat-Top", "Pointy-Top" };
        private int _selectedType = 0;
        private readonly string[] _types = { "Radial", "RectangularOdd", "RectangularEven" };

        private HexOrientation _orientation;
        private HexGridGenerationType _type;
        private int _gridRadius = 1;
        private int _gridWidth = 1;
        private int _gridHeight = 1;
        private Object? _source;

        [MenuItem("Tools/Hex Grid Generator")]
        private static void Init()
        {
            var window = (HexGridGenerator)GetWindow(typeof(HexGridGenerator));
            window.titleContent.text = "Hex Grid Generator";
            window.maxSize = new Vector2(300, 145);
            window.minSize = window.maxSize;
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hex Radius", GUILayout.Width(100));
            _radius = EditorGUILayout.FloatField(_radius, GUILayout.Width(50));
            _radius = Mathf.Clamp(_radius, 1.0f, int.MaxValue);
            _selectedRadiusType = EditorGUILayout.Popup(_selectedRadiusType, _radiusTypes); 
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Orientation", GUILayout.Width(100));
            _selectedOrientation = EditorGUILayout.Popup(_selectedOrientation, _orientations); 
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type", GUILayout.Width(100));
            _selectedType = EditorGUILayout.Popup(_selectedType, _types); 
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Grid Radius", GUILayout.Width(100));
            _gridRadius = EditorGUILayout.IntField(_gridRadius, GUILayout.Width(50));
            _gridRadius = Mathf.Clamp(_gridRadius, 1, int.MaxValue);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Grid Height", GUILayout.Width(100));
            _gridHeight = EditorGUILayout.IntField(_gridHeight, GUILayout.Width(50));
            _gridHeight = Mathf.Clamp(_gridHeight, 1, int.MaxValue);
            GUILayout.Label("Grid Width", GUILayout.Width(80));
            _gridWidth = EditorGUILayout.IntField(_gridWidth, GUILayout.Width(50));
            _gridWidth = Mathf.Clamp(_gridWidth, 1, int.MaxValue);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Grid Object", GUILayout.Width(100));
            _source = EditorGUILayout.ObjectField(_source, typeof(MonoBehaviour), false);
            GUILayout.EndHorizontal();

            var radiusType = _selectedRadiusType == 0 ? HexRadius.Outer : HexRadius.Inner;
            _orientation = _selectedOrientation == 0 ? HexOrientation.FlatTop : HexOrientation.PointyTop;
            _type = _selectedType switch
            {
                0 => HexGridGenerationType.Radial,
                1 => HexGridGenerationType.RectangularOdd,
                2 => HexGridGenerationType.RectangularEven,
                _ => HexGridGenerationType.Radial
            };
            _convertedRadius = ConvertInnerRadiusToOuterByType(radiusType, _radius);
            
            if (GUILayout.Button("Generate"))
            {
                Generate(_orientation);
            }

            GUILayout.EndVertical();
        }

        private void Generate(HexOrientation orientation)
        {
            var selectedHex = Selection.activeGameObject;
            if (selectedHex == null) return;
            if (_source == null) return;
            
            var startedPosition = selectedHex.transform.localPosition;
            var pos = orientation switch
            {
                HexOrientation.FlatTop => ToIntPosition(FromVector3FlatTop(startedPosition, _convertedRadius)),
                HexOrientation.PointyTop => ToIntPosition(FromVector3PointyTop(startedPosition, _convertedRadius)),
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null)
            };

            var objs = _type switch
            {
                HexGridGenerationType.Radial => GetHexesSpiral(pos, _gridRadius),
                HexGridGenerationType.RectangularOdd => GenerateRectangularOdd(_orientation, pos, _gridWidth, _gridHeight),
                HexGridGenerationType.RectangularEven => GenerateRectangularEven(_orientation, pos, _gridWidth, _gridHeight),
                _ => GetHexesSpiral(pos, _gridRadius)
            };

            foreach (var objPos in objs)
            {
                var targetPosition = _orientation switch
                {
                    HexOrientation.FlatTop => FlatTopToVector3(objPos, _convertedRadius),
                    HexOrientation.PointyTop => PointyTopToVector3(objPos, _convertedRadius),
                    _ => throw new ArgumentException("Stranger Things")
                };

                var obj = Instantiate(_source, selectedHex.transform) as MonoBehaviour;
                if (obj != null) obj.transform.localPosition = targetPosition;
            }
        }
        
        private static List<Vector3Int> GenerateRectangularOdd(HexOrientation orientation, Vector3Int pos, int gridWidth, int gridHeight)
        {
            var result = new List<Vector3Int>();
            
            var height = gridHeight;
            var width = gridWidth;
            var oddShift = new Vector3Int(0, 1, -1);
            var evenShift = new Vector3Int(-1, 1, 0);
            var rowShift = new Vector3Int(1, 0, -1);
            
            if (orientation == HexOrientation.FlatTop)
            {
                height = gridWidth;
                width = gridHeight;
                oddShift = new Vector3Int(1, -1, 0);
                evenShift = new Vector3Int(1, 0, -1);
                rowShift = new Vector3Int(0, 1, -1);
            }
            
            var startPos = pos;
            for (var i = 0; i < height; i++)
            {
                if (i != 0 && i % 2 == 0)
                {
                    startPos += evenShift;
                }
                else if (i != 0)
                {
                    startPos += oddShift;
                }
                result.Add(startPos);
                var currentPos = startPos;
                for (var j = 1; j < width; j++)
                {
                    currentPos += rowShift;
                    result.Add(currentPos);
                }
            }

            return result;
        }
        
        private static List<Vector3Int> GenerateRectangularEven(HexOrientation orientation, Vector3Int pos, int gridWidth, int gridHeight)
        {
            var result = new List<Vector3Int>();
            
            var height = gridHeight;
            var width = gridWidth;
            var oddShift = new Vector3Int(-1, 1, 0);
            var evenShift = new Vector3Int(0, 1, -1);
            var rowShift = new Vector3Int(1, 0, -1);
            
            if (orientation == HexOrientation.FlatTop)
            {
                height = gridWidth;
                width = gridHeight;
                oddShift = new Vector3Int(1, 0, -1);
                evenShift = new Vector3Int(1, -1, 0);
                rowShift = new Vector3Int(0, 1, -1);
            }
            
            var startPos = pos;
            for (var i = 0; i < height; i++)
            {
                if (i != 0 && i % 2 == 0)
                {
                    startPos += evenShift;
                }
                else if (i != 0)
                {
                    startPos += oddShift;
                }
                result.Add(startPos);
                var currentPos = startPos;
                for (var j = 1; j < width; j++)
                {
                    currentPos += rowShift;
                    result.Add(currentPos);
                }
            }

            return result;
        }

        private static Vector3 FromVector3PointyTop(Vector3 vector, float hexRadius)
        {
            var x = (Mathf.Sqrt(3)/3 * vector.x - 1.0f/3 * vector.z) / hexRadius;
            var y = 2.0f/3 * vector.z / hexRadius;
            var z = -x - y;

            return new Vector3(x, y, z);
        }

        private static Vector3 FromVector3FlatTop(Vector3 vector, float hexRadius)
        {
            var x = 2.0f / 3 * vector.x / hexRadius;
            var y = (-1.0f / 3 * vector.x + Mathf.Sqrt(3) / 3 * vector.z) / hexRadius;
            var z = -x - y;

            return new Vector3(x, y, z);
        }

        private static Vector3 FlatTopToVector3(Vector3Int position, float hexRadius)
        {
            var x = hexRadius * (3.0f / 2 * position.x);
            var z = hexRadius * (Mathf.Sqrt(3) / 2 * position.x + Mathf.Sqrt(3) * position.y);

            return new Vector3(x, 0.0f, z);
        }

        private static Vector3 PointyTopToVector3(Vector3Int position, float hexRadius)
        {
            var x = hexRadius * (Mathf.Sqrt(3) * position.x + Mathf.Sqrt(3) / 2 * position.y);
            var z = hexRadius * (3.0f / 2 * position.y);

            return new Vector3(x, 0.0f, z);
        }

        private static Vector3Int ToIntPosition(Vector3 position)
        {
            var roundedX = Mathf.RoundToInt(position.x);
            var roundedY = Mathf.RoundToInt(position.y);
            var roundedZ = Mathf.RoundToInt(position.z);

            var xDiff = Mathf.Abs(roundedX - position.x);
            var yDiff = Mathf.Abs(roundedY - position.y);
            var zDiff = Mathf.Abs(roundedZ - position.z);

            if (xDiff > yDiff && xDiff > zDiff)
            {
                roundedX = -roundedY - roundedZ;
            }
            else if (yDiff > zDiff)
            {
                roundedY = -roundedX - roundedZ;
            }
            else
            {
                roundedZ = -roundedX - roundedY;
            }

            return new Vector3Int(roundedX, roundedY, roundedZ);
        }
        
        private static float ConvertInnerRadiusToOuterByType(HexRadius radiusType, float radius)
        {
            return radiusType switch
            {
                HexRadius.Outer => radius,
                HexRadius.Inner => radius * 2 / Mathf.Sqrt(3),
                _ => throw new ArgumentOutOfRangeException(nameof(radiusType), radiusType, null)
            };
        }

        private static List<Vector3Int> GetNeighborsPositions(Vector3Int position)
        {
            return new List<Vector3Int>()
            {
                position + new Vector3Int(0, 1, -1),
                position + new Vector3Int(1, 0, -1),
                position + new Vector3Int(1, -1, 0),
                position + new Vector3Int(0, -1, 1),
                position + new Vector3Int(-1, 0, 1),
                position + new Vector3Int(-1, 1, 0),
            };
        }

        private static IEnumerable<Vector3Int> GetHexesRing(Vector3Int start, int radius)
        {
            var results = new List<Vector3Int>();
            var direction = GetNeighborsPositions(start)[4];
            var scale = direction * radius;
            var hexPosition = start + scale;

            for (var j = 0; j < 6; j++)
            {
                for (var r = 0; r < radius; r++)
                {
                    results.Add(hexPosition);
                    hexPosition = GetNeighborsPositions(hexPosition)[j];
                }
            }

            return results;
        }
        
        private static List<Vector3Int> GetHexesSpiral(Vector3Int start, int radius)
        {
            var results = new List<Vector3Int> { start };
            for (var i = 1; i <= radius; i++)
            {
                results.AddRange(GetHexesRing(start, i));
            }

            return results;
        }
    }
}
