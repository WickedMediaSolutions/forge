using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EvenniaAtlas.Services;

namespace EvenniaAtlas.Dialogs;

public partial class ValidationResultsDialog : Window
{
    public ValidationResultsDialog(List<ValidationIssue> issues)
    {
        InitializeComponent();

        var errors = issues.Count(i => i.Severity == ValidationSeverity.Error);
        var warnings = issues.Count(i => i.Severity == ValidationSeverity.Warning);

        if (issues.Count == 0)
            StatusText.Text = "Validation passed. No issues found.";
        else
            StatusText.Text = $"{issues.Count} issue(s): {errors} error(s), {warnings} warning(s)";

        IssuesList.ItemsSource = issues;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}