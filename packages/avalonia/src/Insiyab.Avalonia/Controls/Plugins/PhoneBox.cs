using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The phone number as it stands: the E.164 value, the country and whether the number is valid.</summary>
public class PhoneChangedEventArgs(RoutedEvent routedEvent, string value, string country, bool isValid) : RoutedEventArgs(routedEvent)
{
    public string Value { get; } = value;
    public string Country { get; } = country;
    public bool IsValid { get; } = isValid;
}

/// <summary>The phone field, the web's <c>data-ins-phone</c>: a country button at the start of an input group and the number written the way it is at home (050 123 4567), while <see cref="Value"/> holds the E.164 number (+966501234567). Countries are raw ISO codes only.</summary>
[TemplatePart("PART_Country", typeof(Button))]
[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_Popup", typeof(Popup))]
[TemplatePart("PART_Search", typeof(TextBox))]
[TemplatePart("PART_List", typeof(ListBox))]
[TemplatePart("PART_None", typeof(TextBlock))]
[PseudoClasses(":open", ":invalid")]
public class PhoneBox : TemplatedControl
{
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<PhoneBox, string?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> DefaultCountryProperty =
        AvaloniaProperty.Register<PhoneBox, string?>(nameof(DefaultCountry));

    public static readonly StyledProperty<string?> PreferredCountriesProperty =
        AvaloniaProperty.Register<PhoneBox, string?>(nameof(PreferredCountries));

    public static readonly StyledProperty<string?> OnlyCountriesProperty =
        AvaloniaProperty.Register<PhoneBox, string?>(nameof(OnlyCountries));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<PhoneBox, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<PhoneBox, string?>(nameof(Locale));

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<PhoneBox, bool>(nameof(IsDropDownOpen));

    public static readonly DirectProperty<PhoneBox, string> CountryProperty =
        AvaloniaProperty.RegisterDirect<PhoneBox, string>(nameof(Country), o => o.Country);

    public static readonly DirectProperty<PhoneBox, string> DialCodeProperty =
        AvaloniaProperty.RegisterDirect<PhoneBox, string>(nameof(DialCode), o => o.DialCode);

    public static readonly DirectProperty<PhoneBox, string> DisplayTextProperty =
        AvaloniaProperty.RegisterDirect<PhoneBox, string>(nameof(DisplayText), o => o.DisplayText);

    public static readonly DirectProperty<PhoneBox, string?> ShownPlaceholderProperty =
        AvaloniaProperty.RegisterDirect<PhoneBox, string?>(nameof(ShownPlaceholder), o => o.ShownPlaceholder);

    public static readonly DirectProperty<PhoneBox, bool> IsValidProperty =
        AvaloniaProperty.RegisterDirect<PhoneBox, bool>(nameof(IsValid), o => o.IsValid);

    public static readonly DirectProperty<PhoneBox, string?> ValidationMessageProperty =
        AvaloniaProperty.RegisterDirect<PhoneBox, string?>(nameof(ValidationMessage), o => o.ValidationMessage);

    public static readonly RoutedEvent<PhoneChangedEventArgs> PhoneChangedEvent =
        RoutedEvent.Register<PhoneBox, PhoneChangedEventArgs>(nameof(PhoneChanged), RoutingStrategies.Bubble);

    private Button? _button;
    private TextBox? _input;
    private Popup? _popup;
    private TextBox? _search;
    private ListBox? _list;
    private TextBlock? _none;

    private PhoneCountry _country = PhoneData.ByIso["SA"];
    private PhoneCountry? _elsewhere;
    private string _nsn = string.Empty;
    private bool _partial, _writing, _left, _loaded, _openedByKey;
    private long _dismissedAt;
    private Key _lastKey;
    private string _last = string.Empty;
    private string _countryIso = "SA", _dial = "966", _display = string.Empty;
    private string? _placeholder, _message;
    private bool _valid;
    private List<PhoneCountryRow> _rows = [];
    private List<PhoneCountryRow> _shown = [];

    /// <summary>The number in E.164 (+966501234567), empty while there is none. Setting it loads it quietly, its country with it.</summary>
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The country to start in (<c>data-ins-phone-country</c>); otherwise the number's own, else Saudi Arabia.</summary>
    public string? DefaultCountry { get => GetValue(DefaultCountryProperty); set => SetValue(DefaultCountryProperty, value); }

    /// <summary>ISO codes listed first, above the rest (<c>data-ins-phone-preferred="EG,JO"</c>).</summary>
    public string? PreferredCountries { get => GetValue(PreferredCountriesProperty); set => SetValue(PreferredCountriesProperty, value); }

    /// <summary>The only ISO codes offered (<c>data-ins-phone-only="SA,AE"</c>); a number from elsewhere is refused with a message.</summary>
    public string? OnlyCountries { get => GetValue(OnlyCountriesProperty); set => SetValue(OnlyCountriesProperty, value); }

    /// <summary>Unset, the field shows an example number from the chosen country.</summary>
    public string? PlaceholderText { get => GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    /// <summary>"ar" or "en" for names and messages; unset, the direction decides.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <summary>The chosen country's ISO code.</summary>
    public string Country { get => _countryIso; private set => SetAndRaise(CountryProperty, ref _countryIso, value); }

    public string DialCode { get => _dial; private set => SetAndRaise(DialCodeProperty, ref _dial, value); }

    /// <summary>The number as the field shows it.</summary>
    public string DisplayText { get => _display; private set => SetAndRaise(DisplayTextProperty, ref _display, value); }

    public string? ShownPlaceholder { get => _placeholder; private set => SetAndRaise(ShownPlaceholderProperty, ref _placeholder, value); }

    public bool IsValid { get => _valid; private set => SetAndRaise(IsValidProperty, ref _valid, value); }

    public string? ValidationMessage { get => _message; private set => SetAndRaise(ValidationMessageProperty, ref _message, value); }

    /// <summary>The value changed: the web's <c>ins:phone</c>.</summary>
    public event EventHandler<PhoneChangedEventArgs>? PhoneChanged
    {
        add => AddHandler(PhoneChangedEvent, value);
        remove => RemoveHandler(PhoneChangedEvent, value);
    }

    /// <summary>The countries offered, in no particular order.</summary>
    public IReadOnlyList<PhoneCountry> AllowedCountries => Allowed();

    internal IReadOnlyList<PhoneCountryRow> VisibleRows => _shown;

    private bool Arabic => PluginText.IsArabic(this, Locale);

    private List<PhoneCountry> Allowed()
    {
        var only = PhoneData.ParseList(OnlyCountries);
        return only.Count > 0 ? only : PhoneData.Countries;
    }

    private bool IsAllowed(PhoneCountry c) => Allowed().Contains(c);

    private PhoneCountry StartCountry(string? value)
    {
        var list = Allowed();
        var c = PhoneData.Find(DefaultCountry);
        if (c is null && !string.IsNullOrEmpty(value))
        {
            var d = PhoneData.Read(value, list[0]);
            if (d.Intl && !d.Partial) c = d.Country;
        }
        if (c is null && Locale is { } loc && loc.LastIndexOf('-') is var i and > 0) c = PhoneData.Find(loc[(i + 1)..]);
        if (c is null || !list.Contains(c)) c = list.Contains(PhoneData.ByIso["SA"]) ? PhoneData.ByIso["SA"] : list[0];
        return c;
    }

    private string E164 => _nsn.Length > 0 ? "+" + _country.Dial + _nsn : string.Empty;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_input is not null)
        {
            _input.RemoveHandler(KeyDownEvent, OnInputKey);
            _input.TextChanged -= OnInputText;
            _input.LostFocus -= OnInputLeave;
        }
        if (_button is not null)
        {
            _button.Click -= OnButtonClick;
            _button.RemoveHandler(KeyDownEvent, OnButtonKey);
            _button.RemoveHandler(TextInputEvent, OnButtonText);
        }
        if (_search is not null)
        {
            _search.TextChanged -= OnSearchText;
            _search.RemoveHandler(KeyDownEvent, OnSearchKey);
        }
        if (_list is not null)
        {
            _list.Tapped -= OnListTapped;
            _list.PointerMoved -= OnListPointerMoved;
            _list.LayoutUpdated -= OnListLaidOut;
        }
        if (_popup is not null) _popup.Closed -= OnPopupClosed;

        base.OnApplyTemplate(e);
        _button = e.NameScope.Find<Button>("PART_Country");
        _input = e.NameScope.Find<TextBox>("PART_Input");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _search = e.NameScope.Find<TextBox>("PART_Search");
        _list = e.NameScope.Find<ListBox>("PART_List");
        _none = e.NameScope.Find<TextBlock>("PART_None");

        if (_input is not null)
        {
            _input.AddHandler(KeyDownEvent, OnInputKey, RoutingStrategies.Tunnel);
            _input.TextChanged += OnInputText;
            _input.LostFocus += OnInputLeave;
            TextInputOptions.SetContentType(_input, TextInputContentType.Number);
        }
        if (_button is not null)
        {
            _button.Click += OnButtonClick;
            _button.AddHandler(KeyDownEvent, OnButtonKey, RoutingStrategies.Tunnel);
            _button.AddHandler(TextInputEvent, OnButtonText, RoutingStrategies.Tunnel);
        }
        if (_search is not null)
        {
            _search.TextChanged += OnSearchText;
            _search.AddHandler(KeyDownEvent, OnSearchKey, RoutingStrategies.Tunnel);
        }
        if (_list is not null)
        {
            _list.Tapped += OnListTapped;
            _list.PointerMoved += OnListPointerMoved;
        }
        if (_popup is not null) _popup.Closed += OnPopupClosed;

        if (!_loaded)
        {
            _loaded = true;
            Load(Value, StartCountry(Value));
        }
        Show(null);
        Paint();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
        {
            if (_writing) return;
            if (!_loaded) return;
            Load(Value, null);
        }
        else if (change.Property == DefaultCountryProperty || change.Property == OnlyCountriesProperty)
        {
            if (!_loaded) return;
            if (_nsn.Length == 0 && PhoneData.Find(DefaultCountry) is { } c && IsAllowed(c)) SetCountry(c);
            else if (!IsAllowed(_country)) SetCountry(StartCountry(null));
        }
        else if (change.Property == PlaceholderTextProperty || change.Property == LocaleProperty || change.Property == FlowDirectionProperty)
        {
            Paint();
            Check();
        }
        else if (change.Property == IsDropDownOpenProperty)
        {
            if (change.GetNewValue<bool>()) Open();
            else Close(false);
        }
    }

    private void Paint()
    {
        Country = _country.Iso;
        DialCode = _country.Dial;
        ShownPlaceholder = PlaceholderText ?? PhoneData.Placeholder(_country);
        if (_button is not null)
            AutomationPropertiesHelper.SetName(_button, (Arabic ? "الدولة" : "Country") + ": " + _country.Name(Arabic) + " +" + _country.Dial);
    }

    private void SetCountry(PhoneCountry c)
    {
        if (_country == c) return;
        _country = c;
        Paint();
    }

    private bool Check()
    {
        string? msg = null;
        if (_elsewhere is not null)
            msg = Arabic ? $"لا تُقبل هنا أرقام {_elsewhere.ArabicName}." : $"Numbers from {_elsewhere.EnglishName} are not accepted here.";
        else if (_partial)
            msg = Arabic ? "اكتب رمز الدولة كاملًا بعد +." : "Enter the full country code after +.";
        else if (_nsn.Length > 0 && !PhoneData.Valid(_nsn, _country))
            msg = Arabic ? $"اكتب رقم هاتف صحيحًا في {_country.ArabicName}." : $"Enter a valid phone number for {_country.EnglishName}.";
        ValidationMessage = msg;
        IsValid = msg is null && _nsn.Length > 0;
        var invalid = _left && msg is not null;
        PseudoClasses.Set(":invalid", invalid);
        if (_input is not null) DataValidationErrors.SetErrors(_input, invalid ? new object[] { msg! } : null);
        return msg is null;
    }

    // After the national number changed: the value, the check, and the announcement when the value is new.
    private void Commit(bool quiet)
    {
        var value = E164;
        _writing = true;
        try { SetCurrentValue(ValueProperty, value); }
        finally { _writing = false; }
        var ok = Check();
        if (!quiet && value != _last)
        {
            _last = value;
            RaiseEvent(new PhoneChangedEventArgs(PhoneChangedEvent, value, _country.Iso, ok && value.Length > 0));
        }
        if (quiet) _last = value;
    }

    // at: how many digits of the number the caret was after, or null to leave the caret alone.
    private void Show(int? at)
    {
        var f = PhoneData.Format(_nsn, _country);
        SetText(f.Text);
        if (at is { } a && _input is { IsFocused: true })
        {
            var pos = a <= 0 ? (f.At.Count > 0 ? f.At[0] - 1 : f.Text.Length) : f.At[Math.Min(a, f.At.Count) - 1];
            _input.CaretIndex = pos;
        }
    }

    private void SetText(string text)
    {
        DisplayText = text;
        if (_input is null || _input.Text == text) return;
        _writing = true;
        try { _input.Text = text; }
        finally { _writing = false; }
    }

    // A value given to the field: in markup, from Value, by a reset.
    private void Load(string? value, PhoneCountry? fallback)
    {
        var r = PhoneData.Read(value ?? string.Empty, fallback ?? _country);
        var ok = !r.Intl || (!r.Partial && IsAllowed(r.Country));
        if (r.Intl && ok) SetCountry(r.Country);
        else if (fallback is not null) SetCountry(fallback);
        _nsn = ok ? r.Nsn : string.Empty;
        _partial = false;
        _elsewhere = null;
        SetText(PhoneData.Format(_nsn, _country).Text);
        Commit(true);
        Paint();
    }

    private void OnInputKey(object? sender, KeyEventArgs e)
    {
        _lastKey = e.Key;
        if (e.Key == Key.Enter) Leave();
    }

    // As the person types: a national number is regrouped on every key with the caret kept after its digit; one with + waits for its country.
    private void OnInputText(object? sender, TextChangedEventArgs e)
    {
        if (_writing || _input is null) return;
        var raw = _input.Text ?? string.Empty;
        var caret = Math.Clamp(_input.CaretIndex, 0, raw.Length);
        var before = PluginText.Latin(raw[..caret]).Count(char.IsAsciiDigit);
        var res = PhoneData.Read(raw, _country);
        _partial = res.Partial;
        _elsewhere = null;
        var key = _lastKey;
        _lastKey = Key.None;
        if (res.Intl)
        {
            if (res.Partial || IsAllowed(res.Country))
            {
                if (!res.Partial) SetCountry(res.Country);
                _nsn = res.Nsn;
            }
            else
            {
                _elsewhere = res.Country;
                _nsn = string.Empty;
            }
            DisplayText = raw;
            Commit(false);
            return;
        }
        var nsn = res.Nsn;
        var at = Math.Max(0, before - res.Cut);
        // A Backspace that took out a space or a bracket meant the digit before it.
        if (nsn == _nsn && key == Key.Back && at > 0) { nsn = nsn[..(at - 1)] + nsn[at..]; at--; }
        else if (nsn == _nsn && key == Key.Delete && at < nsn.Length) nsn = nsn[..at] + nsn[(at + 1)..];
        _nsn = nsn;
        // A lone trunk 0 stays on screen: it is the start of a number, not nothing.
        if (nsn.Length == 0 && res.Typed.Length > 0)
        {
            DisplayText = raw;
            _writing = true;
            try { SetCurrentValue(ValueProperty, string.Empty); }
            finally { _writing = false; }
            ValidationMessage = null;
            return;
        }
        Show(at);
        Commit(false);
    }

    // Another window taking the keyboard is not leaving the field.
    private void OnInputLeave(object? sender, RoutedEventArgs e)
    {
        if (!WindowActivity.GetIsInactive(this)) Leave();
    }

    private void Leave()
    {
        if (_input is null) return;
        var res = PhoneData.Read(_input.Text, _country);
        _partial = res.Partial;
        _elsewhere = null;
        if (res.Intl && !res.Partial)
        {
            if (IsAllowed(res.Country)) { SetCountry(res.Country); _nsn = res.Nsn; }
            else { _elsewhere = res.Country; _nsn = string.Empty; }
        }
        else if (!res.Intl) _nsn = res.Nsn;
        if (!_partial && _elsewhere is null) Show(null);
        _left = true;
        Commit(false);
    }

    // ---- the list ----

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!IsDropDownOpen && Stopwatch.GetElapsedTime(_dismissedAt).TotalMilliseconds < 300) return;
        SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
    }

    private void OnButtonKey(object? sender, KeyEventArgs e)
    {
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (e.Key is Key.Down or Key.Up or Key.F4 || (alt && e.Key == Key.Down))
        {
            e.Handled = true;
            _openedByKey = true;
            SetCurrentValue(IsDropDownOpenProperty, true);
        }
    }

    // A letter typed on the closed button opens the list with that letter already in the search box.
    private void OnButtonText(object? sender, TextInputEventArgs e)
    {
        if (IsDropDownOpen || string.IsNullOrWhiteSpace(e.Text)) return;
        e.Handled = true;
        _openedByKey = true;
        SetCurrentValue(IsDropDownOpenProperty, true);
        var seed = e.Text;
        Dispatcher.UIThread.Post(() =>
        {
            if (_search is null) return;
            _search.Text = seed;
            _search.CaretIndex = seed.Length;
        });
    }

    private void Open()
    {
        if (_popup is null || _list is null) { return; }
        BuildList();
        if (_search is not null)
        {
            _search.PlaceholderText = Arabic ? "ابحث عن دولة أو رمز" : "Search for a country or code";
            _search.Text = string.Empty;
        }
        if (_none is not null) _none.Text = Arabic ? "لا دولة بهذا الاسم" : "No country by that name";
        Filter();
        _popup.IsOpen = true;
        _list.LayoutUpdated -= OnListLaidOut;
        _list.LayoutUpdated += OnListLaidOut;
        PseudoClasses.Set(":open", true);
        _button?.Classes.Set("open", true);
        Dispatcher.UIThread.Post(() => _search?.Focus());
    }

    private void Close(bool refocus)
    {
        if (_list is not null) _list.LayoutUpdated -= OnListLaidOut;
        if (_popup is { IsOpen: true }) _popup.IsOpen = false;
        PseudoClasses.Set(":open", false);
        _button?.Classes.Set("open", false);
        if (IsDropDownOpen) SetCurrentValue(IsDropDownOpenProperty, false);
        if (refocus) _button?.Focus(_openedByKey ? NavigationMethod.Tab : NavigationMethod.Pointer);
        _openedByKey = false;
    }

    // A click outside closes the list before the button sees its own click, which must not open it again.
    private void OnPopupClosed(object? sender, EventArgs e)
    {
        if (IsDropDownOpen) _dismissedAt = Stopwatch.GetTimestamp();
        Close(false);
    }

    private void BuildList()
    {
        var arabic = Arabic;
        var countries = Allowed().ToList();
        StringComparer cmp;
        try { cmp = StringComparer.Create(CultureInfo.GetCultureInfo(arabic ? "ar" : "en"), false); }
        catch (CultureNotFoundException) { cmp = StringComparer.Ordinal; }
        countries.Sort((a, b) => cmp.Compare(a.Name(arabic), b.Name(arabic)));
        var preferred = PhoneData.ParseList(PreferredCountries).Where(countries.Contains).ToList();
        var rows = new List<PhoneCountryRow>(countries.Count);
        foreach (var c in preferred) rows.Add(new PhoneCountryRow(c, arabic, c == _country, c == preferred[^1]));
        foreach (var c in countries)
            if (!preferred.Contains(c)) rows.Add(new PhoneCountryRow(c, arabic, c == _country, false));
        _rows = rows;
    }

    // By name in Arabic or English, by the two-letter code, or by the dial code with or without its +.
    private void OnSearchText(object? sender, TextChangedEventArgs e) => Filter();

    private void Filter()
    {
        if (_list is null) return;
        var arabic = Arabic;
        var raw = PluginText.Latin(_search?.Text).Trim();
        var q = ArabicSearch.Normalize(raw);
        var isDial = raw.Length > 0 && raw.TrimStart('+').Length > 0 && raw.TrimStart('+').All(char.IsAsciiDigit) && raw.LastIndexOf('+') <= 0;
        var digits = isDial ? raw.Replace("+", string.Empty) : string.Empty;
        _shown = q.Length == 0
            ? _rows
            : _rows
                .Where(r => isDial ? r.Country.Dial.StartsWith(digits, StringComparison.Ordinal) : r.SearchKey.Contains(q, StringComparison.Ordinal))
                .Select(r => r.EndsPreferred ? new PhoneCountryRow(r.Country, arabic, r.Weight == FontWeight.Bold, false) : r)
                .ToList();
        _list.ItemsSource = _shown;
        if (_none is not null) _none.IsVisible = _shown.Count == 0;
        Activate(q.Length > 0 ? _shown.FirstOrDefault() : _shown.FirstOrDefault(r => r.Country == _country) ?? _shown.FirstOrDefault());
    }

    // The list opens with the current country in its middle; the arrows and the search scroll only as far as they must.
    private void OnListLaidOut(object? sender, EventArgs e)
    {
        if (_list is not { Scroll: ScrollViewer { Viewport.Height: > 0 } }) return;
        _list.LayoutUpdated -= OnListLaidOut;
        CentreActive();
    }

    private void CentreActive()
    {
        if (_list is not { SelectedItem: { } row, Scroll: ScrollViewer scroll }) return;
        _list.ScrollIntoView(row);
        if (_list.ContainerFromItem(row) is not Control item || item.TranslatePoint(default, scroll) is not { } at) return;
        var y = scroll.Offset.Y + at.Y + item.Bounds.Height / 2 - scroll.Viewport.Height / 2;
        scroll.Offset = new Vector(scroll.Offset.X, Math.Clamp(y, 0, Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height)));
    }

    private void Activate(PhoneCountryRow? row)
    {
        if (_list is null) return;
        _list.SelectedItem = row;
        if (row is not null) _list.ScrollIntoView(row);
    }

    private void Move(int by)
    {
        if (_list is null || _shown.Count == 0) return;
        var at = _list.SelectedIndex < 0 ? (by > 0 ? -1 : _shown.Count) : _list.SelectedIndex;
        Activate(_shown[Math.Clamp(at + by, 0, _shown.Count - 1)]);
    }

    private void OnSearchKey(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                e.Handled = true;
                Move(1);
                break;
            case Key.Up:
                e.Handled = true;
                Move(-1);
                break;
            case Key.PageDown:
                e.Handled = true;
                Move(8);
                break;
            case Key.PageUp:
                e.Handled = true;
                Move(-8);
                break;
            case Key.Enter:
                e.Handled = true;
                if (_list?.SelectedItem is PhoneCountryRow row) Choose(row);
                break;
            case Key.Escape:
                e.Handled = true;
                Close(true);
                break;
            case Key.Tab:
                Close(false);
                break;
        }
    }

    private static PhoneCountryRow? RowAt(RoutedEventArgs e) =>
        (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(true)?.DataContext as PhoneCountryRow;

    private void OnListTapped(object? sender, TappedEventArgs e)
    {
        if (RowAt(e) is { } row) Choose(row);
    }

    // The highlight follows the pointer, so only one row is ever lit.
    private void OnListPointerMoved(object? sender, PointerEventArgs e)
    {
        if (RowAt(e) is { } row && _list is not null && !ReferenceEquals(row, _list.SelectedItem)) _list.SelectedItem = row;
    }

    private void Choose(PhoneCountryRow row)
    {
        Close(false);
        SetCountry(row.Country);
        Show(null);
        Commit(false);
        _input?.Focus();
    }

    /// <summary>Picks a country, as a choice from the list does.</summary>
    public void SelectCountry(string iso)
    {
        if (PhoneData.Find(iso) is not { } c || !IsAllowed(c)) return;
        SetCountry(c);
        Show(null);
        Commit(false);
    }
}

/// <summary>A row of the phone field's country list: the code pill, the name and the dial code.</summary>
public sealed class PhoneCountryRow
{
    internal PhoneCountryRow(PhoneCountry country, bool arabic, bool current, bool endsPreferred)
    {
        Country = country;
        Iso = country.Iso;
        Name = country.Name(arabic);
        Dial = "+" + country.Dial;
        Weight = current ? FontWeight.Bold : FontWeight.Normal;
        EndsPreferred = endsPreferred;
        SearchKey = PhoneData.SearchKey(country);
    }

    public PhoneCountry Country { get; }

    /// <summary>The ISO code, "SA".</summary>
    public string Iso { get; }

    /// <summary>The name in the field's language.</summary>
    public string Name { get; }

    /// <summary>The dial code, "+966".</summary>
    public string Dial { get; }

    /// <summary>Bold for the current country, else normal.</summary>
    public FontWeight Weight { get; }

    /// <summary>The last preferred row while not searching: it draws the separator.</summary>
    public bool EndsPreferred { get; }

    internal string SearchKey { get; }
}

internal static class AutomationPropertiesHelper
{
    public static void SetName(Control control, string name) => global::Avalonia.Automation.AutomationProperties.SetName(control, name);
}
