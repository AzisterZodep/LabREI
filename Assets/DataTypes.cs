using System;
using UnityEngine;
using UnityEngine.UI; // Required for Image, Text components

// Represents a single tile/grid item
[Serializable]
public class TileData
{
    public string id; // Unique identifier for the tile (optional)
    public string imageUrl; // URL or path to the image for the tile
    public string linkUrl; // URL to open when the tile is clicked
    public string displayText; // Text to display on the tile
    public string customColor1; // For your --color1, e.g., "rgba(100, 120, 140, 0.5)"
    public string customColor2; // For your --color2
    public string customColor3; // For your --color3
}

// Represents the root of your JSON data
[Serializable]
public class GridData
{
    public TileData[] tiles;
    // You could also add other data like backgrounds, colors arrays here
    public string[] backgrounds;
    public string[] bgPositions;
    public string[] colors1;
    public string[] colors2;
    public string[] colors3;
}