using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace SpecifiedExtensionFileMove
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 選択ボタンによるフォルダ選択および画面表示
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private async void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            Windows.Storage.StorageFolder folder =
                await folderPicker.PickSingleFolderAsync();

            if (folder == null)
            {
                return;
            }

            SavedFolderPathLabel.Text = folder.Path.ToString();
        }

        /// <summary>
        /// フォルダドロップ時のファイルパス取得と表示
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private async void FoldersListView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                // フォルダのパス一覧を取得する(ファイルは除外)
                var items = await e.DataView.GetStorageItemsAsync();
                var folderPaths = items.Where(x => Directory.Exists(x.Path)).Select(x => x.Path).ToArray();
                FoldersListView.ItemsSource = folderPaths;
                SetPickupListView(folderPaths);
            }
        }

        /// <summary>
        /// PickupListViewにファイルパスをセットする
        /// </summary>
        /// <param name="folderPaths">フォルダパスリスト</param>
        private void SetPickupListView(string[] folderPaths)
        {
            var fileList = new List<string>();
            List<string> patterns = GetPatternFromExtensions();

            // searchOption オプション [ AllDirectories…サブフォルダーも検索する / TopDirectoryOnly…直下のフォルダのみ検索する ]
            var searchOption = (bool)SubFoldercheckBox.IsChecked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // フォルダを展開する
            foreach (string path in folderPaths)
            {
                var files = Directory.EnumerateFiles(path, "*.*", searchOption);
                var filteringFile = files.Where(file => patterns.Any(pattern => file.ToLower().EndsWith(pattern))).ToArray();

                foreach (string filePath in filteringFile)
                {
                    fileList.Add(filePath);
                }
            }
            // ファイル一覧を格納
            PickupListView.ItemsSource = fileList;
        }

        /// <summary>
        /// 拡張子パターンの取得
        /// </summary>
        /// <returns>パターンリスト</returns>
        private List<string> GetPatternFromExtensions()
        {
            // チェックする拡張子の格納
            List<string> patterns = new List<string>();

            // 拡張子チェックボックスの格納
            CheckBox[] extensionCheckBoxes = { AviCheckBox, MkvCheckBox, Mp4CheckBox, WmvCheckBox, JpgCheckBox, PngCheckBox, ZipCheckBox };

            var pattern = string.Empty;
            foreach (CheckBox checkBox in extensionCheckBoxes)
            {
                pattern = OnOffCheckExtensions(checkBox);
                if (pattern != string.Empty) { patterns.Add(pattern); }
            }

            // 拡張子テキストボックスに設定されている拡張子の格納
            if (SpecifiedTextBox.Text != string.Empty)
            {
                string[] lines = SpecifiedTextBox.Text.Split(',');
                foreach (string data in lines)
                {
                    patterns.Add("." + data.Trim());
                }
            }
            return patterns;
        }

        /// <summary>
        /// チェックされた拡張子の格納
        /// </summary>
        /// <param name="checkBox">チェックボックスオブジェクト</param>
        /// <returns>.拡張子</returns>
        private string OnOffCheckExtensions(CheckBox checkBox)
        {
            var extensionString = string.Empty;
            if ((bool)checkBox.IsChecked) { extensionString = "." + checkBox.Content.ToString(); }
            return extensionString;
        }

        /// <summary>
        /// ドラッグが重なった際の表示アイコン変更
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void FoldersListView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Link;
        }

        /// <summary>
        /// 実行ボタンクリックイベントによる、ピックアップファイルの指定フォルダへの移動もしくはコピー
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private async void ExecButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存先フォルダ指定確認
            if (SavedFolderPathLabel.Text == string.Empty)
            {
                await new Windows.UI.Popups.MessageDialog("保存先フォルダが指定されていません。", "保存先フォルダ存在確認").ShowAsync();
                return;
            }

            // 保存先フォルダ存在確認
            if (!Directory.Exists(SavedFolderPathLabel.Text))
            {
                await new Windows.UI.Popups.MessageDialog("指定された保存先フォルダが存在しません。", "保存先フォルダ存在確認").ShowAsync();
                return;
            }

            // ピックアップファイル指定確認
            if (PickupListView.Items.Count < 1)
            {
                await new Windows.UI.Popups.MessageDialog("ピックアップファイルが指定されていません。", "ファイル指定確認").ShowAsync();
                return;
            }

            // ファイル存在確認
            foreach (object item in PickupListView.Items)
            {
                if (!File.Exists(item.ToString()))
                {
                    await new Windows.UI.Popups.MessageDialog("存在しないファイルがあります。", "ファイル存在確認").ShowAsync();
                    return;
                }
            }

            // 確認ダイアログ
            var msg = new Windows.UI.Popups.MessageDialog("処理を継続します", "継続確認");
            msg.Commands.Add(new Windows.UI.Popups.UICommand("継続"));
            msg.Commands.Add(new Windows.UI.Popups.UICommand("キャンセル"));

            var res = await msg.ShowAsync();

            try
            {
                if (res.Label == "継続")
                {
                    try
                    {
                        var copyMode = (bool)CopyCheckBox.IsChecked;
                        foreach (object item in PickupListView.Items)
                        {
                            var targetPath = SavedFolderPathLabel.Text + "\\" + Path.GetFileName(item.ToString());
                            if (copyMode)
                            {
                                File.Copy(item.ToString(), targetPath, true);
                            }
                            else
                            {
                                File.Move(item.ToString(), targetPath);
                            }
                        }
                    }
                    catch
                    {
                        await new Windows.UI.Popups.MessageDialog("ファイル移動に失敗しました。\n既にファイルが存在していないか、ファイルがロックされていないか確認してください。", "例外").ShowAsync();
                        return;
                    }

                    if ((bool)DeleteFolderCheckBox.IsChecked)
                    {
                        try
                        {
                            foreach (object item in FoldersListView.Items)
                            {
                                Directory.Delete(item.ToString(), true);
                            }
                        }
                        catch
                        {
                            await new Windows.UI.Popups.MessageDialog("フォルダ削除に失敗しました。\nファイルがロックされていないか確認してください。", "例外").ShowAsync();
                            return;
                        }
                    }
                    await new Windows.UI.Popups.MessageDialog("完了しました。", "実行結果").ShowAsync();
                }
                else
                {
                    await new Windows.UI.Popups.MessageDialog("処理を中断しました。", "キャンセル").ShowAsync();
                    return;
                }
            }
            catch
            {
                await new Windows.UI.Popups.MessageDialog("システムエラーが発生しました。", "例外").ShowAsync();
                return;
            }
        }

        /// <summary>
        /// 拡張子テキストボックスの変更イベントによる拡張子再取得
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void ExtensionCheckBoxes_Click(object sender, RoutedEventArgs e)
        {
           SetPickupListView((string[])FoldersListView.ItemsSource);
        }

        /// <summary>
        /// 拡張子テキストボックスからカーソルが離脱した際の拡張子取得
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void SpecifiedTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // HACK: 変更の有無をチェックして処理させることが望ましい
                SetPickupListView((string[])FoldersListView.ItemsSource);
        }
    }
}