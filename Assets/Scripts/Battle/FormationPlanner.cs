using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FormationPlanner
{
    public static List<(HeroUnit unit, GridTile tile)> GenerateFormation(
        List<HeroUnit> units,
        GridTile[,] battleGrid,
        bool isTeamA)
    {
        int gridSizeX = battleGrid.GetLength(0);
        int gridSizeY = battleGrid.GetLength(1);

        int frontRow = isTeamA ? 1 : gridSizeY - 2;
        int backRow = isTeamA ? 0 : gridSizeY - 1;

        var frontliners = units.Where(u => u.heroData.heroRole == HeroRole.Frontline).ToList();
        var backliners = units.Where(u => u.heroData.heroRole == HeroRole.Backline).ToList();
        var flexUnits = units.Where(u => u.heroData.heroRole == HeroRole.Flexible).ToList();

        List<(HeroUnit, GridTile)> assignments = new();
        HashSet<GridTile> usedTiles = new();

        AssignToStaggeredRow(frontliners, frontRow, evenFirst: true);
        AssignToStaggeredRow(backliners, backRow, evenFirst: false);
        AssignToStaggeredRow(flexUnits, frontRow, evenFirst: true); // fallback to front

        return assignments;

        // 👇 Helper local method for staggered placement
        void AssignToStaggeredRow(List<HeroUnit> group, int row, bool evenFirst)
        {
            foreach (var unit in group.OrderBy(u => u.GridPosition.x))
            {
                int desiredCol = Mathf.Clamp(unit.GridPosition.x, 0, gridSizeX - 1);
                GridTile bestTile = FindStaggeredFreeTile(desiredCol, row, battleGrid, usedTiles, evenFirst);

                if (bestTile != null)
                {
                    assignments.Add((unit, bestTile));
                    usedTiles.Add(bestTile);
                }
                else
                {
                    Debug.LogWarning($"⚠️ No free tile found in staggered pattern for unit: {unit.heroData.heroName}");
                }
            }
        }
    }

    private static GridTile FindStaggeredFreeTile(
        int desiredCol,
        int row,
        GridTile[,] grid,
        HashSet<GridTile> used,
        bool evenFirst)
    {
        int gridSizeX = grid.GetLength(0);

        // Pass 1: Try staggered pattern (even or odd first)
        List<int> staggerCols = new();
        for (int i = 0; i < gridSizeX; i++)
        {
            if (evenFirst && i % 2 == 0) staggerCols.Add(i);
            if (!evenFirst && i % 2 != 0) staggerCols.Add(i);
        }

        foreach (int col in staggerCols.OrderBy(c => Mathf.Abs(c - desiredCol)))
        {
            GridTile tile = grid[col, row];
            if (tile != null && !used.Contains(tile))
                return tile;
        }

        // Pass 2: Fallback to any column (tight pack)
        for (int col = 0; col < gridSizeX; col++)
        {
            GridTile tile = grid[col, row];
            if (tile != null && !used.Contains(tile))
                return tile;
        }

        return null;
    }
}
