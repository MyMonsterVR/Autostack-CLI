namespace AutoStack_CLI;

public class InteractiveMenu<T>
{
    private readonly List<T> _items;
    private readonly Func<T, string> _displaySelector;
    private readonly Func<int, Task<List<T>>>? _onPageChange;
    private readonly int _itemsPerPage;
    private readonly int _totalPages;
    private readonly string? _title;

    private int _selectedIndex = 0;
    private int _currentPage = 1;
    private bool _hasSelectedOption = false;
    private T? _selectedItem;
    private int _menuStartLine = 0;
    private int _previousSelectedIndex = -1;

    /// <summary>
    /// Creates an interactive menu with arrow key navigation and optional pagination
    /// </summary>
    /// <param name="items">Initial list of items to display</param>
    /// <param name="displaySelector">Function to convert item to display string</param>
    /// <param name="itemsPerPage">Number of items to show per page (default: 10)</param>
    /// <param name="totalPages">Total number of pages (optional, for API pagination)</param>
    /// <param name="onPageChange">Optional async callback when user changes pages (left/right arrows)</param>
    /// <param name="title">Optional title/header shown above the menu</param>
    public InteractiveMenu(
        List<T> items,
        Func<T, string> displaySelector,
        int itemsPerPage = 10,
        int? totalPages = null,
        Func<int, Task<List<T>>>? onPageChange = null,
        string? title = null)
    {
        _items = items;
        _displaySelector = displaySelector;
        _itemsPerPage = itemsPerPage;
        _onPageChange = onPageChange;
        _totalPages = totalPages ?? (int)Math.Ceiling((double)items.Count / itemsPerPage);
        _title = title;
    }

    /// <summary>
    /// Displays the menu and returns the selected item (or default if cancelled)
    /// </summary>
    public async Task<T?> ShowAsync()
    {
        if (_items.Count == 0)
        {
            Console.WriteLine("No items to display");
            return default;
        }

        Console.CursorVisible = false;

        try
        {
            await RenderMenu(fullRender: true);

            while (!_hasSelectedOption)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (_selectedIndex > 0)
                        {
                            _previousSelectedIndex = _selectedIndex;
                            _selectedIndex--;
                            await RenderMenu(fullRender: false);
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        var maxIndex = Math.Min(_itemsPerPage, _items.Count) - 1;
                        if (_selectedIndex < maxIndex)
                        {
                            _previousSelectedIndex = _selectedIndex;
                            _selectedIndex++;
                            await RenderMenu(fullRender: false);
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        if (_currentPage > 1)
                        {
                            _currentPage--;
                            _selectedIndex = 0;

                            if (_onPageChange != null)
                            {
                                Console.WriteLine("\nLoading previous page...");
                                _items.Clear();
                                _items.AddRange(await _onPageChange(_currentPage));
                            }

                            await RenderMenu(fullRender: true);
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (_onPageChange != null && _currentPage < _totalPages)
                        {
                            _currentPage++;
                            _selectedIndex = 0;

                            Console.WriteLine("\nLoading next page...");
                            _items.Clear();
                            _items.AddRange(await _onPageChange(_currentPage));

                            await RenderMenu(fullRender: true);
                        }
                        break;

                    case ConsoleKey.Enter:
                        _selectedItem = _items[_selectedIndex];
                        _hasSelectedOption = true;
                        break;

                    case ConsoleKey.Escape:
                        _hasSelectedOption = true;
                        break;
                }
            }

            return _selectedItem;
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    private Task RenderMenu(bool fullRender)
    {
        var itemsToShow = _items.Take(_itemsPerPage).ToList();

        if (fullRender)
        {
            RenderFullMenu(itemsToShow);
        }
        else
        {
            RenderPartialMenu(itemsToShow);
        }

        return Task.CompletedTask;
    }

    private void RenderFullMenu(List<T> itemsToShow)
    {
        Console.Clear();

        // Header
        Console.WriteLine("================================================================================");
        Console.WriteLine("  Use UP/DOWN to navigate, LEFT/RIGHT for pages, Enter to select, Esc to cancel");

        if (_onPageChange != null && _totalPages > 1)
        {
            Console.WriteLine($"  Page {_currentPage} of {_totalPages}");
        }

        Console.WriteLine("================================================================================\n");

        if (!string.IsNullOrEmpty(_title))
        {
            Console.WriteLine(_title);
            Console.WriteLine();
        }

        // Store where menu items start
        _menuStartLine = Console.CursorTop;

        for (var i = 0; i < itemsToShow.Count; i++)
        {
            RenderMenuItem(itemsToShow[i], i == _selectedIndex);
            Console.WriteLine();
        }
    }

    private void RenderPartialMenu(List<T> itemsToShow)
    {
        // Update previous selection (unselect it)
        if (_previousSelectedIndex >= 0 && _previousSelectedIndex < itemsToShow.Count)
        {
            Console.SetCursorPosition(0, _menuStartLine + _previousSelectedIndex);
            RenderMenuItem(itemsToShow[_previousSelectedIndex], false);
        }

        // Update new selection (select it)
        if (_selectedIndex >= 0 && _selectedIndex < itemsToShow.Count)
        {
            Console.SetCursorPosition(0, _menuStartLine + _selectedIndex);
            RenderMenuItem(itemsToShow[_selectedIndex], true);
        }
    }

    private void RenderMenuItem(T item, bool isSelected)
    {
        var text = isSelected ? $" > {_displaySelector(item)}" : $"   {_displaySelector(item)}";

        var targetWidth = Console.WindowWidth - 1;
        if (text.Length > targetWidth)
        {
            text = text.Substring(0, targetWidth);
        }
        else if (text.Length < targetWidth)
        {
            text = text.PadRight(targetWidth);
        }

        if (isSelected)
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(text);
            Console.ResetColor();
        }
        else
        {
            Console.Write(text);
        }
    }
}
