using Aml.BOM.Import.Domain.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aml.BOM.Import.Domain.Entities;

public class NewMakeItem : INotifyPropertyChanged
{
    private string _itemDescription = string.Empty;
    private string _productLine = string.Empty;
    private string _productType = "F";
    private string _procurement = "M";
    private string _standardUnitOfMeasure = "EACH";
    private string _subProductFamily = string.Empty;
    private bool _stagedItem;
    private bool _coated;
    private bool _goldenStandard;
    private bool _isEdited;

    public int Id { get; set; }
    
    // System fields (not editable)
    public string ImportFileName { get; set; } = string.Empty;
    public DateTime ImportFileDate { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    
    // Editable fields
    public string ItemDescription
    {
        get => _itemDescription;
        set
        {
            if (_itemDescription != value)
            {
                _itemDescription = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    public string ProductLine
    {
        get => _productLine;
        set
        {
            if (_productLine != value)
            {
                _productLine = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    public string ProductType
    {
        get => _productType;
        set
        {
            if (_productType != value)
            {
                _productType = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    public string Procurement
    {
        get => _procurement;
        set
        {
            if (_procurement != value)
            {
                _procurement = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    public string StandardUnitOfMeasure
    {
        get => _standardUnitOfMeasure;
        set
        {
            if (_standardUnitOfMeasure != value)
            {
                _standardUnitOfMeasure = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    public string SubProductFamily
    {
        get => _subProductFamily;
        set
        {
            if (_subProductFamily != value)
            {
                _subProductFamily = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    public bool StagedItem
    {
        get => _stagedItem;
        set
        {
            if (_stagedItem != value)
            {
                _stagedItem = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    public bool Coated
    {
        get => _coated;
        set
        {
            if (_coated != value)
            {
                _coated = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    public bool GoldenStandard
    {
        get => _goldenStandard;
        set
        {
            if (_goldenStandard != value)
            {
                _goldenStandard = value;
                IsEdited = true;
                OnPropertyChanged();
            }
        }
    }

    // Status tracking
    public bool IsEdited
    {
        get => _isEdited;
        set
        {
            _isEdited = value;
            OnPropertyChanged();
        }
    }

    public bool IsIntegrated { get; set; }
    public DateTime? IntegratedDate { get; set; }
    public string? IntegratedBy { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    // Legacy fields (for compatibility)
    public string? LongDescription { get; set; }
    public string? DrawingNumber { get; set; }
    public string? Revision { get; set; }
    public string? ProductGroup { get; set; }
    public string? Category { get; set; }
    public decimal? StandardCost { get; set; }
    public ItemIntegrationStatus Status { get; set; }
    public string IdentifiedBy { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
