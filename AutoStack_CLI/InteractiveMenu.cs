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
            // Full render - clear screen and draw everything
            Console.Clear();

            // Header
            Console.WriteLine("================================================================================");
            Console.WriteLine("  Use UP/DOWN to navigate, LEFT/RIGHT for pages, Enter to select, Esc to cancel");

            if (_onPageChange != null && _totalPages > 1)
            {
                Console.WriteLine($"  Page {_currentPage} of {_totalPages}");
            }

            Console.WriteLine("================================================================================\n");

            // Title (if provided)
            if (!string.IsNullOrEmpty(_title))
            {
                Console.WriteLine(_title);
                Console.WriteLine();
            }

            // Store where menu items start
            _menuStartLine = Console.CursorTop;

            // Items
            for (var i = 0; i < itemsToShow.Count; i++)
            {
                RenderMenuItem(itemsToShow[i], i == _selectedIndex);
            }

            Console.WriteLine();
        }
        else
        {
            // Partial render - only update changed lines
            if (_previousSelectedIndex >= 0 && _previousSelectedIndex < itemsToShow.Count)
            {
                // Redraw previously selected item (now unselected)
                Console.SetCursorPosition(0, _menuStartLine + _previousSelectedIndex);
                RenderMenuItem(itemsToShow[_previousSelectedIndex], false);
            }

            // Redraw newly selected item
            if (_selectedIndex >= 0 && _selectedIndex < itemsToShow.Count)
            {
                Console.SetCursorPosition(0, _menuStartLine + _selectedIndex);
                RenderMenuItem(itemsToShow[_selectedIndex], true);
            }
        }

        return Task.CompletedTask;
    }

    private void RenderMenuItem(T item, bool isSelected)
    {
        if (isSelected)
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write($" > {_displaySelector(item)}");

            // Clear to end of line to avoid leftover characters
            var currentLeft = Console.CursorLeft;
            var width = Console.WindowWidth;
            if (currentLeft < width)
            {
                Console.Write(new string(' ', width - currentLeft - 1));
            }

            Console.WriteLine();
            Console.ResetColor();
        }
        else
        {
            Console.Write($"   {_displaySelector(item)}");

            // Clear to end of line
            var currentLeft = Console.CursorLeft;
            var width = Console.WindowWidth;
            if (currentLeft < width)
            {
                Console.Write(new string(' ', width - currentLeft - 1));
            }

            Console.WriteLine();
        }
    }
}
