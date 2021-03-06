﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using PoESkillTree.Utils;
using PoESkillTree.Common.ViewModels;

namespace PoESkillTree.Controls.Dialogs.ViewModels
{
    /// <summary>
    /// View model used for selecting a path to a file or directory.
    /// </summary>
    public class FileSelectorViewModel : ErrorInfoViewModel<string?>
    {
        private string _filePath;
        private string _sanitizedFilePath;
        private readonly bool _isFolderPicker;
        private readonly string? _validationSubPath;
        private readonly Func<string, string?> _additionalValidationFunc;
        private readonly bool _useRelativePaths;

        public string Message { get; }

        public string FilePath
        {
            get { return _filePath; }
            set { SetProperty(ref _filePath, value); }
        }

        /// <summary>
        /// Gets a variant of the last <see cref="FilePath"/> that was valid that does not end with a directory
        /// separator.
        /// </summary>
        public string SanitizedFilePath
        {
            get { return _sanitizedFilePath; }
            private set { SetProperty(ref _sanitizedFilePath, value); }
        }

        public ICommand SelectFileCommand { get; }

        public bool IsCancelable { get; }

#pragma warning disable CS8618 // _filePath and _sanitizedFilePath are set through FilePath
        public FileSelectorViewModel(string title, string message, FileSelectorDialogSettings settings)
#pragma warning restore
        {
            if (!settings.IsFolderPicker && !string.IsNullOrEmpty(settings.ValidationSubPath))
                throw new ArgumentException("ValidationSubPath may only be given if IsFolderPicker is true",
                    nameof(settings));
            DisplayName = title;
            Message = message;
            IsCancelable = settings.IsCancelable;
            _isFolderPicker = settings.IsFolderPicker;
            _validationSubPath = settings.ValidationSubPath;
            _additionalValidationFunc = settings.AdditionalValidationFunc;
            _useRelativePaths = AppData.IsPortable;
            FilePath = settings.DefaultPath;
            SelectFileCommand = new RelayCommand(SelectFile);
        }

        private void SelectFile()
        {
            var path = Path.GetFullPath(SanitizedFilePath);
            if (_isFolderPicker)
            {
                var dialog = new FolderBrowserDialog
                {
                    SelectedPath = path,
                };
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                path = dialog.SelectedPath;
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    InitialDirectory = Path.GetDirectoryName(path),
                    FileName = Path.GetFileName(path),
                };
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                path = dialog.FileName;
            }
            FilePath = _useRelativePaths ? AppData.ToRelativePath(path) : path;
        }

        protected override IEnumerable<string> ValidateProperty(string propertyName)
        {
            if (propertyName != nameof(FilePath))
                return Enumerable.Empty<string>();
            var trimmed = PathEx.TrimTrailingDirectorySeparators(FilePath);
            if (PathEx.IsPathValid(trimmed, out var message, mustBeDirectory: _isFolderPicker, mustBeFile: !_isFolderPicker,
                mustBeAbsolute: !_useRelativePaths))
            {
                if (!string.IsNullOrEmpty(_validationSubPath))
                {
                    PathEx.IsPathValid(Path.Combine(trimmed, _validationSubPath), out message);
                }
                if (message is null)
                {
                    message = _additionalValidationFunc(trimmed);
                }
                SanitizedFilePath = _useRelativePaths ? AppData.ToRelativePath(trimmed) : trimmed;
            }
            return message is null ? Enumerable.Empty<string>() : new[] {message};
        }
    }
}