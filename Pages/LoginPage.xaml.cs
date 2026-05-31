using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ManufacturingApp.Helpers;
using ManufacturingApp.Models;
using ManufacturingApp.Pages;

namespace ManufacturingApp.Pages
{
    public partial class LoginPage : Page
    {
        private const int COLS = 3;
        private const int ROWS = 2;
        private const int PIECES = COLS * ROWS;

        private int[] _shuffledSource;
        private int[] _targetPlacement;
        private int _selectedSourcePiece = -1;
        private bool[] _sourcePlaced;
        private readonly Brush[] _pieceColors =
        {
            new SolidColorBrush(Color.FromRgb(52,  152, 219)),
            new SolidColorBrush(Color.FromRgb(46,  204, 113)),
            new SolidColorBrush(Color.FromRgb(231, 76,  60)),
            new SolidColorBrush(Color.FromRgb(243, 156, 18)),
            new SolidColorBrush(Color.FromRgb(155, 89,  182)),
            new SolidColorBrush(Color.FromRgb(52,  73,  94)),
        };
        private readonly string[] _pieceSymbols = { "▲", "■", "●", "◆", "★", "▶" };

        private bool _captchaPassed = false;

        // LOGIN
        private int _localAttempts = 0;      // captcha fail streak
        private string _lastLogin = "";

        public LoginPage()
        {
            InitializeComponent();
            InitializePuzzle();
        }

        // CAPTCHA

        private void InitializePuzzle()
        {
            _captchaPassed = false;
            _selectedSourcePiece = -1;
            _sourcePlaced = new bool[PIECES];
            _targetPlacement = Enumerable.Repeat(-1, PIECES).ToArray();

            _shuffledSource = Enumerable.Range(0, PIECES).ToArray();
            ShuffleArray(_shuffledSource);

            BtnLogin.IsEnabled = false;
            TbCaptchaStatus.Text = "";
            TbCaptchaStatus.Foreground = Brushes.Gray;

            BuildSourceGrid();
            BuildTargetGrid();
        }

        private void BuildSourceGrid()
        {
            PuzzleSource.Children.Clear();
            // Source shows pieces in shuffled visual order
            for (int i = 0; i < PIECES; i++)
            {
                int pieceIdx = _shuffledSource[i]; 
                var cell = CreatePieceCell(pieceIdx, false);
                cell.Tag = pieceIdx;
                cell.MouseLeftButtonDown += SourceCell_Click;
                PuzzleSource.Children.Add(cell);
            }
        }

        private void BuildTargetGrid()
        {
            PuzzleTarget.Children.Clear();
            for (int i = 0; i < PIECES; i++)
            {
                int cellIdx = i;
                var cell = CreateEmptyTargetCell(cellIdx);
                PuzzleTarget.Children.Add(cell);
            }
        }

        private Border CreatePieceCell(int pieceIdx, bool isPlaced)
        {
            var b = new Border
            {
                Background = isPlaced
                    ? new SolidColorBrush(Color.FromRgb(220, 220, 220))
                    : _pieceColors[pieceIdx],
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(1),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.White,
                Cursor = isPlaced ? System.Windows.Input.Cursors.Arrow
                                  : System.Windows.Input.Cursors.Hand
            };

            var tb = new TextBlock
            {
                Text = isPlaced ? "" : _pieceSymbols[pieceIdx],
                FontSize = 20,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };

            var numTb = new TextBlock
            {
                Text = isPlaced ? "" : (pieceIdx + 1).ToString(),
                FontSize = 9,
                Foreground = new SolidColorBrush(Colors.White) { Opacity = 0.7 },
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 3, 2)
            };

            var grid = new Grid();
            grid.Children.Add(tb);
            grid.Children.Add(numTb);
            b.Child = grid;
            return b;
        }

        private Border CreateEmptyTargetCell(int cellIdx)
        {
            int placedPiece = _targetPlacement[cellIdx];
            bool hasContent = placedPiece >= 0;

            var b = new Border
            {
                Background = hasContent
                    ? _pieceColors[placedPiece]
                    : new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(1),
                BorderThickness = new Thickness(2),
                BorderBrush = _selectedSourcePiece >= 0
                    ? new SolidColorBrush(Color.FromRgb(26, 58, 92))
                    : Brushes.LightGray,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = cellIdx
            };

            if (hasContent)
            {
                var sp = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                sp.Children.Add(new TextBlock
                {
                    Text = _pieceSymbols[placedPiece],
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                sp.Children.Add(new TextBlock
                {
                    Text = (placedPiece + 1).ToString(),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                b.Child = sp;
            }
            else if (_selectedSourcePiece >= 0)
            {
                b.Child = new TextBlock
                {
                    Text = "＋",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            b.MouseLeftButtonDown += TargetCell_Click;
            return b;
        }

        private void SourceCell_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!(sender is Border b)) return;
            int pieceIdx = (int)b.Tag;

            if (_sourcePlaced[pieceIdx]) return;

            _selectedSourcePiece = pieceIdx;

            // Highlight selection
            for (int i = 0; i < PuzzleSource.Children.Count; i++)
            {
                var cell = (Border)PuzzleSource.Children[i];
                int cellPieceIdx = (int)cell.Tag; // Fix: check against Tag, not iteration index i
                cell.BorderBrush = cellPieceIdx == pieceIdx
                    ? new SolidColorBrush(Color.FromRgb(243, 156, 18))
                    : Brushes.White;
                cell.BorderThickness = cellPieceIdx == pieceIdx
                    ? new Thickness(3) : new Thickness(2);
            }

            RefreshTargetGrid();
        }

        private void TargetCell_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!(sender is Border b)) return;
            int cellIdx = (int)b.Tag;

            if (_selectedSourcePiece < 0) return;

            if (_targetPlacement[cellIdx] >= 0)
            {
                int existing = _targetPlacement[cellIdx];
                _sourcePlaced[existing] = false;
            }

            _targetPlacement[cellIdx] = _selectedSourcePiece;
            _sourcePlaced[_selectedSourcePiece] = true;
            _selectedSourcePiece = -1;

            foreach (Border cell in PuzzleSource.Children)
            {
                cell.BorderBrush = Brushes.White;
                cell.BorderThickness = new Thickness(2);
            }

            RefreshSourceGrid();
            RefreshTargetGrid();
        }

        private void RefreshSourceGrid()
        {
            PuzzleSource.Children.Clear();
            for (int i = 0; i < PIECES; i++)
            {
                int pieceIdx = _shuffledSource[i];
                var newCell = CreatePieceCell(pieceIdx, _sourcePlaced[pieceIdx]);
                newCell.Tag = pieceIdx;
                if (!_sourcePlaced[pieceIdx])
                    newCell.MouseLeftButtonDown += SourceCell_Click;
                PuzzleSource.Children.Add(newCell);
            }
        }

        private void RefreshTargetGrid()
        {
            PuzzleTarget.Children.Clear();
            for (int i = 0; i < PIECES; i++)
            {
                var cell = CreateEmptyTargetCell(i);
                PuzzleTarget.Children.Add(cell);
            }
        }

        private void BtnCheckCaptcha_Click(object sender, RoutedEventArgs e)
        {
            // Check all cells filled
            if (_targetPlacement.Any(v => v < 0))
            {
                TbCaptchaStatus.Text = "❗ Заполните все ячейки";
                TbCaptchaStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                return;
            }

            // Fix: Check correct order
            bool correct = true;
            for (int i = 0; i < PIECES; i++)
            {
                if (_targetPlacement[i] != i)
                {
                    correct = false;
                    break;
                }
            }

            if (correct)
            {
                _captchaPassed = true;
                _localAttempts = 0;
                TbCaptchaStatus.Text = "✔ Пазл собран верно!";
                TbCaptchaStatus.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                BtnLogin.IsEnabled = true;
                BtnCheckCaptcha.IsEnabled = false;
            }
            else
            {
                _localAttempts++;
                TbCaptchaStatus.Text =
                    $"✘ Неверно. Попытка {_localAttempts}/3";
                TbCaptchaStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));

                if (_localAttempts >= 3)
                {
                    // Block by login
                    if (!string.IsNullOrWhiteSpace(_lastLogin))
                    {
                        var u = DatabaseHelper.GetUserByLogin(_lastLogin.Trim());
                        if (u != null)
                        {
                            DatabaseHelper.BlockUser(u.UserID);
                            ShowError("Вы заблокированы. Обратитесь к администратору");
                        }
                        else
                        {
                            ShowError("Пазл неверно собран 3 раза. Сбросьте и попробуйте заново.");
                        }
                    }
                    else
                    {
                        ShowError("Пазл неверно собран 3 раза подряд. Сбросьте и попробуйте снова.");
                    }
                    BtnCheckCaptcha.IsEnabled = false;
                    BtnLogin.IsEnabled = false;
                }
                else
                {
                    InitializePuzzle();
                }
            }
        }

        private void BtnResetCaptcha_Click(object sender, RoutedEventArgs e)
        {
            _localAttempts = 0;
            _captchaPassed = false;
            BtnCheckCaptcha.IsEnabled = true;
            BtnLogin.IsEnabled = false;
            HideBanners();
            InitializePuzzle();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            HideBanners();

            string login = TbLogin.Text.Trim();
            string password = PbPassword.Password;
            _lastLogin = login;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Поля «Логин» и «Пароль» обязательны для заполнения.");
                return;
            }

            if (!_captchaPassed)
            {
                ShowError("Сначала пройдите проверку пазлом.");
                return;
            }

            User user;
            try
            {
                user = DatabaseHelper.GetUserByLogin(login);
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения к БД: {ex.Message}");
                return;
            }

            if (user == null)
            {
                ShowError("Вы ввели неверный логин или пароль. " +
                          "Пожалуйста проверьте ещё раз введенные данные");
                return;
            }

            if (user.IsBlocked)
            {
                ShowError("Вы заблокированы. Обратитесь к администратору");
                return;
            }

            string hash = PasswordHelper.Hash(password);

            bool pwOk = string.Equals(user.PasswordHash, hash,
                                      StringComparison.OrdinalIgnoreCase);

            if (!pwOk)
            {
                user.FailedAttempts++;
                DatabaseHelper.IncrementFailedAttempts(user.UserID);

                if (user.FailedAttempts >= 3)
                {
                    DatabaseHelper.BlockUser(user.UserID);
                    ShowError("Вы заблокированы. Обратитесь к администратору");
                }
                else
                {
                    ShowError("Вы ввели неверный логин или пароль. " +
                              "Пожалуйста проверьте ещё раз введенные данные");
                }
                return;
            }

            // Success
            DatabaseHelper.ResetFailedAttempts(user.UserID);
            AppSession.CurrentUser = user;

            ShowSuccess("Вы успешно авторизовались");

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                MainWindow.Instance.ShowNavBar(user.Login, user.RoleName);
                if (user.IsAdmin)
                    NavigationService.Navigate(new AdminPage());
                else
                    NavigationService.Navigate(new UserPage());
            };
            timer.Start();
        }

        // ════════════════════ HELPERS ════════════════════

        private void ShowError(string msg)
        {
            TbError.Text = "⚠  " + msg;
            BannerError.Visibility = Visibility.Visible;
            BannerSuccess.Visibility = Visibility.Collapsed;
        }

        private void ShowSuccess(string msg)
        {
            TbSuccess.Text = "✔  " + msg;
            BannerSuccess.Visibility = Visibility.Visible;
            BannerError.Visibility = Visibility.Collapsed;
        }

        private void HideBanners()
        {
            BannerError.Visibility = Visibility.Collapsed;
            BannerSuccess.Visibility = Visibility.Collapsed;
        }

        private static void ShuffleArray(int[] arr)
        {
            var rng = new Random();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}