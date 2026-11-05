using System.Collections;
using System.Collections.Specialized;
using Avalonia.Utilities;

namespace Insiyab.Avalonia.Controls;

/// <summary>A timeline's items with a day heading before the first item of each day, worked out again whenever the items change.</summary>
internal sealed class TimelineDays : IList, INotifyCollectionChanged, IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
{
    private readonly Func<object?, DateTime?> _dayOf;
    private readonly List<object?> _rows = [];

    public TimelineDays(IEnumerable source, Func<object?, DateTime?> dayOf)
    {
        Source = source;
        _dayOf = dayOf;
        Build();
        if (source is INotifyCollectionChanged changes) WeakEvents.CollectionChanged.Subscribe(changes, this);
    }

    public IEnumerable Source { get; }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _rows.Count;
    public bool IsReadOnly => true;
    public bool IsFixedSize => false;
    public bool IsSynchronized => false;
    public object SyncRoot => this;

    public object? this[int index] { get => _rows[index]; set => throw new NotSupportedException(); }

    public int Add(object? value) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public void Insert(int index, object? value) => throw new NotSupportedException();
    public void Remove(object? value) => throw new NotSupportedException();
    public void RemoveAt(int index) => throw new NotSupportedException();
    public bool Contains(object? value) => _rows.Contains(value);
    public int IndexOf(object? value) => _rows.IndexOf(value);
    public void CopyTo(Array array, int index) => ((ICollection)_rows).CopyTo(array, index);
    public IEnumerator GetEnumerator() => _rows.GetEnumerator();

    public void OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs e)
    {
        Build();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void Build()
    {
        _rows.Clear();
        DateTime? last = null;
        foreach (var item in Source)
        {
            if (_dayOf(item) is { } at && at.Date != last)
            {
                last = at.Date;
                _rows.Add(new TimelineDayMark(at.Date));
            }
            _rows.Add(item);
        }
    }
}

/// <summary>Where a day heading goes in a <see cref="TimelineDays"/> list; the timeline makes a <see cref="TimelineDay"/> for it.</summary>
internal sealed class TimelineDayMark(DateTime day)
{
    public DateTime Day { get; } = day;
}
