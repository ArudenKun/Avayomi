﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Generator.Attributes;

namespace Desktop.Controls.SpaceGrid;

[AvaloniaProperty("RowSpacing", typeof(double))]
[AvaloniaProperty("ColumnSpacing", typeof(double))]
public partial class SpaceGrid : Grid
{
    public SpaceGrid()
    {
        Children.CollectionChanged += ChildrenOnCollectionChanged;
    }

    /// <summary>
    ///     Returns an enumerable of all the grid's row definitions, <u>excluding</u> spacing rows.
    /// </summary>
    public IEnumerable<RowDefinition> UserDefinedRowDefinitions =>
        RowDefinitions.Where(definition => definition is not ISpacingDefinition);

    /// <summary>
    ///     Returns an enumerable of all the grid's column definitions, <u>excluding</u> spacing columns.
    /// </summary>
    public IEnumerable<ColumnDefinition> UserDefinedColumnDefinitions =>
        ColumnDefinitions.Where(definition => definition is not ISpacingDefinition);

    private void UpdateSpacedRows()
    {
        var userRowDefinitions = UserDefinedRowDefinitions.ToList(); // User-defined rows (e.g. the ones defined in XAML files)
        var actualRowDefinitions = new RowDefinitions(); // User-defined + spacing rows

        int currentUserDefinition = 0,
            currentActualDefinition = 0;

        while (currentUserDefinition < userRowDefinitions.Count)
        {
            if (currentActualDefinition % 2 == 0) // Even rows are user-defined rows (0, 2, 4, 6, 8, 10, ...)
            {
                actualRowDefinitions.Add(userRowDefinitions[currentUserDefinition]);
                currentUserDefinition++;
            }
            else // Odd rows are spacing rows (1, 3, 5, 7, 9, 11, ...)
            {
                actualRowDefinitions.Add(new SpacingRowDefinition(RowSpacing));
            }

            currentActualDefinition++;
        }

        RowDefinitions = actualRowDefinitions;
        RowDefinitions.CollectionChanged += delegate
        {
            UpdateSpacedRows();
        };
    }

    private void UpdateSpacedColumns()
    {
        var userColumnDefinitions = UserDefinedColumnDefinitions.ToList(); // User-defined columns (e.g. the ones defined in XAML files)
        var actualColumnDefinitions = new ColumnDefinitions(); // User-defined + spacing columns

        int currentUserDefinition = 0,
            currentActualDefinition = 0;

        while (currentUserDefinition < userColumnDefinitions.Count)
        {
            if (currentActualDefinition % 2 == 0) // Even columns are user-defined columns (0, 2, 4, 6, 8, 10, ...)
            {
                actualColumnDefinitions.Add(userColumnDefinitions[currentUserDefinition]);
                currentUserDefinition++;
            }
            else // Odd columns are spacing columns (1, 3, 5, 7, 9, 11, ...)
            {
                actualColumnDefinitions.Add(new SpacingColumnDefinition(ColumnSpacing));
            }

            currentActualDefinition++;
        }

        ColumnDefinitions = actualColumnDefinitions;
        ColumnDefinitions.CollectionChanged += delegate
        {
            UpdateSpacedColumns();
        };
    }

    private void RecalculateRowSpacing()
    {
        foreach (var spacingRow in RowDefinitions.OfType<ISpacingDefinition>())
            spacingRow.Spacing = RowSpacing;
    }

    private void RecalculateColumnSpacing()
    {
        foreach (var spacingColumn in ColumnDefinitions.OfType<ISpacingDefinition>())
            spacingColumn.Spacing = ColumnSpacing;
    }

    #region Override Methods

    protected override void OnInitialized()
    {
        base.OnInitialized();

        RowDefinitions.CollectionChanged += delegate
        {
            UpdateSpacedRows();
        };
        ColumnDefinitions.CollectionChanged += delegate
        {
            UpdateSpacedColumns();
        };

        UpdateSpacedRows();
        UpdateSpacedColumns();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(RowSpacing):
                RecalculateRowSpacing();
                break;

            case nameof(ColumnSpacing):
                RecalculateColumnSpacing();
                break;
        }
    }

    #endregion Override methods

    #region Event Handlers

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (
            e.Action != NotifyCollectionChangedAction.Add
            && e.Action != NotifyCollectionChangedAction.Replace
        )
            return;

        if (e.NewItems == null)
            return;

        foreach (Control item in e.NewItems)
            item.Initialized += ItemOnInitialized;
    }

    private void ItemOnInitialized(object? sender, EventArgs e)
    {
        if (sender is not Control item)
            return;

        item.Initialized -= ItemOnInitialized;

        SetRow(item, GetRow(item) * 2); // 1 -> 2 or 2 -> 4
        SetRowSpan(item, GetRowSpan(item) * 2 - 1); // 2 -> 3 or 3 -> 5

        SetColumn(item, GetColumn(item) * 2); // 1 -> 2 or 2 -> 4
        SetColumnSpan(item, GetColumnSpan(item) * 2 - 1); // 2 -> 3 or 3 -> 5
    }

    #endregion
}
