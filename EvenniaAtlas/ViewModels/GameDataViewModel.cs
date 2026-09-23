using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.ViewModels;

public class GameDataViewModel : BaseViewModel
{
    private MapProject? _project;

    public Action? MarkDirtyCallback { get; set; }
    private void MarkDirty() => MarkDirtyCallback?.Invoke();

    // ---- Counters ----
    private int _damageTypeCounter;
    private int _factionCounter;
    private int _professionCounter;
    private int _speciesCounter;
    private int _alignmentCounter;

    // ---- Tab index ----
    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetField(ref _selectedTabIndex, value))
            {
                MarkDirty();
                OnPropertyChanged(nameof(ActiveCollection));
                OnPropertyChanged(nameof(ActiveFiltered));
                OnPropertyChanged(nameof(ActiveSelectedEntry));
                OnPropertyChanged(nameof(HasActiveSelectedEntry));
                OnPropertyChanged(nameof(ActiveTabLabel));
                RefreshActiveFilter();
            }
        }
    }

    public string ActiveTabLabel => _selectedTabIndex switch
    {
        0 => "Damage Types",
        1 => "Factions",
        2 => "Professions",
        3 => "Species",
        4 => "Alignments",
        _ => ""
    };

    // ---- Observable collections ----
    public ObservableCollection<GameDataEntryModel> DamageTypes { get; } = new();
    public ObservableCollection<GameDataEntryModel> Factions { get; } = new();
    public ObservableCollection<GameDataEntryModel> Professions { get; } = new();
    public ObservableCollection<GameDataEntryModel> Species { get; } = new();
    public ObservableCollection<GameDataEntryModel> Alignments { get; } = new();

    public ObservableCollection<GameDataEntryModel> FilteredDamageTypes { get; } = new();
    public ObservableCollection<GameDataEntryModel> FilteredFactions { get; } = new();
    public ObservableCollection<GameDataEntryModel> FilteredProfessions { get; } = new();
    public ObservableCollection<GameDataEntryModel> FilteredSpecies { get; } = new();
    public ObservableCollection<GameDataEntryModel> FilteredAlignments { get; } = new();

    // ---- Selected entries ----
    private GameDataEntryModel? _selectedDamageType;
    public GameDataEntryModel? SelectedDamageType
    {
        get => _selectedDamageType;
        set
        {
            var old = _selectedDamageType;
            if (SetField(ref _selectedDamageType, value))
            {
                if (old != null) MarkDirty();
            }
        }
    }

    private GameDataEntryModel? _selectedFaction;
    public GameDataEntryModel? SelectedFaction
    {
        get => _selectedFaction;
        set
        {
            var old = _selectedFaction;
            if (SetField(ref _selectedFaction, value))
            {
                if (old != null) MarkDirty();
            }
        }
    }

    private GameDataEntryModel? _selectedProfession;
    public GameDataEntryModel? SelectedProfession
    {
        get => _selectedProfession;
        set
        {
            var old = _selectedProfession;
            if (SetField(ref _selectedProfession, value))
            {
                if (old != null) MarkDirty();
            }
        }
    }

    private GameDataEntryModel? _selectedSpecies;
    public GameDataEntryModel? SelectedSpecies
    {
        get => _selectedSpecies;
        set
        {
            var old = _selectedSpecies;
            if (SetField(ref _selectedSpecies, value))
            {
                if (old != null) MarkDirty();
            }
        }
    }

    private GameDataEntryModel? _selectedAlignment;
    public GameDataEntryModel? SelectedAlignment
    {
        get => _selectedAlignment;
        set
        {
            var old = _selectedAlignment;
            if (SetField(ref _selectedAlignment, value))
            {
                if (old != null) MarkDirty();
            }
        }
    }

    // ---- Active (tab-dependent) properties ----
    public ObservableCollection<GameDataEntryModel> ActiveCollection => _selectedTabIndex switch
    {
        0 => DamageTypes,
        1 => Factions,
        2 => Professions,
        3 => Species,
        4 => Alignments,
        _ => DamageTypes
    };
public ObservableCollection<GameDataEntryModel> ActiveFiltered => _selectedTabIndex switch
    {
        0 => FilteredDamageTypes,
        1 => FilteredFactions,
        2 => FilteredProfessions,
        3 => FilteredSpecies,
        4 => FilteredAlignments,
        _ => FilteredDamageTypes
    };

    public GameDataEntryModel? ActiveSelectedEntry
    {
        get => _selectedTabIndex switch
        {
            0 => SelectedDamageType,
            1 => SelectedFaction,
            2 => SelectedProfession,
            3 => SelectedSpecies,
            4 => SelectedAlignment,
            _ => null
        };
        set
        {
            switch (_selectedTabIndex)
            {
                case 0: SelectedDamageType = value; break;
                case 1: SelectedFaction = value; break;
                case 2: SelectedProfession = value; break;
                case 3: SelectedSpecies = value; break;
                case 4: SelectedAlignment = value; break;
            }
        }
    }

    public bool HasActiveSelectedEntry => ActiveSelectedEntry != null;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
                RefreshActiveFilter();
        }
    }

    public ICommand AddEntryCommand { get; }
    public ICommand DeleteEntryCommand { get; }

    public GameDataViewModel()
    {
        AddEntryCommand = new RelayCommand(_ => AddEntry(), _ => _project != null);
        DeleteEntryCommand = new RelayCommand(_ => DeleteEntry(), _ => HasActiveSelectedEntry);
    }

    public void SetProject(MapProject project)
    {
        _project = project;

        _damageTypeCounter = project.DamageTypes.Count > 0
            ? project.DamageTypes.Max(d => ExtractNumericSuffix(d.Id, "damage_type_"))
            : 0;
        _factionCounter = project.Factions.Count > 0
            ? project.Factions.Max(f => ExtractNumericSuffix(f.Id, "faction_"))
            : 0;
        _professionCounter = project.Professions.Count > 0
            ? project.Professions.Max(p => ExtractNumericSuffix(p.Id, "profession_"))
            : 0;
        _speciesCounter = project.Species.Count > 0
            ? project.Species.Max(s => ExtractNumericSuffix(s.Id, "species_"))
            : 0;
        _alignmentCounter = project.Alignments.Count > 0
            ? project.Alignments.Max(a => ExtractNumericSuffix(a.Id, "alignment_"))
            : 0;

        DamageTypes.Clear();
        foreach (var dt in project.DamageTypes) DamageTypes.Add(dt);

        Factions.Clear();
        foreach (var f in project.Factions) Factions.Add(f);

        Professions.Clear();
        foreach (var p in project.Professions) Professions.Add(p);

        Species.Clear();
        foreach (var s in project.Species) Species.Add(s);

        Alignments.Clear();
        foreach (var a in project.Alignments) Alignments.Add(a);

        SelectedDamageType = null;
        SelectedFaction = null;
        SelectedProfession = null;
        SelectedSpecies = null;
        SelectedAlignment = null;

        RefreshActiveFilter();
    }

    private static int ExtractNumericSuffix(string id, string prefix)
    {
        if (!id.StartsWith(prefix)) return 0;
        var suffix = id.Substring(prefix.Length);
        return int.TryParse(suffix, out var num) ? num : 0;
    }

    private void AddEntry()
    {
        if (_project == null) return;

        string prefix;
        ObservableCollection<GameDataEntryModel> collection;
        List<GameDataEntryModel> canonical;

        switch (_selectedTabIndex)
        {
            case 0:
                _damageTypeCounter++;
                prefix = "damage_type_";
                collection = DamageTypes;
                canonical = _project.DamageTypes;
                break;
            case 1:
                _factionCounter++;
                prefix = "faction_";
                collection = Factions;
                canonical = _project.Factions;
                break;
            case 2:
                _professionCounter++;
                prefix = "profession_";
                collection = Professions;
                canonical = _project.Professions;
                break;
            case 3:
                _speciesCounter++;
                prefix = "species_";
                collection = Species;
                canonical = _project.Species;
                break;
            case 4:
                _alignmentCounter++;
                prefix = "alignment_";
                collection = Alignments;
                canonical = _project.Alignments;
                break;
            default:
                _damageTypeCounter++;
                prefix = "damage_type_";
                collection = DamageTypes;
                canonical = _project.DamageTypes;
                break;
        }

        var counter = _selectedTabIndex switch
        {
            0 => _damageTypeCounter,
            1 => _factionCounter,
            2 => _professionCounter,
            3 => _speciesCounter,
            4 => _alignmentCounter,
            _ => _damageTypeCounter
        };

        var newEntry = new GameDataEntryModel
        {
            Id = $"{prefix}{counter:D4}",
            Name = string.Empty,
            Description = string.Empty,
        };

        canonical.Add(newEntry);
        collection.Add(newEntry);
        ActiveSelectedEntry = newEntry;
        RefreshActiveFilter();
        MarkDirty();
    }

    private void DeleteEntry()
    {
        if (_project == null || ActiveSelectedEntry == null) return;

        ObservableCollection<GameDataEntryModel> collection;
        List<GameDataEntryModel> canonical;

        switch (_selectedTabIndex)
        {
            case 0:
                collection = DamageTypes;
                canonical = _project.DamageTypes;
                break;
            case 1:
                collection = Factions;
                canonical = _project.Factions;
                break;
            case 2:
                collection = Professions;
                canonical = _project.Professions;
                break;
            case 3:
                collection = Species;
                canonical = _project.Species;
                break;
            case 4:
                collection = Alignments;
                canonical = _project.Alignments;
                break;
            default:
                collection = DamageTypes;
                canonical = _project.DamageTypes;
                break;
        }

        var entry = ActiveSelectedEntry;
        canonical.Remove(entry);
        collection.Remove(entry);
        ActiveSelectedEntry = null;
        RefreshActiveFilter();
        MarkDirty();
    }

    private void RefreshActiveFilter()
    {
        var source = ActiveCollection;
        var target = ActiveFiltered;
        target.Clear();

        var filter = _searchText?.Trim() ?? string.Empty;
        foreach (var entry in source)
        {
            if (string.IsNullOrEmpty(filter) ||
                entry.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                entry.Description.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                target.Add(entry);
            }
        }
    }
}