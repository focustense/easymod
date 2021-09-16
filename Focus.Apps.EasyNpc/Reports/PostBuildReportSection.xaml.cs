using System;
using System.Windows;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc.Reports
{
    public enum PostBuildReportSectionStatus { OK, Warning, Error }

    /// <summary>
    /// Interaction logic for PostBuildReportSection.xaml
    /// </summary>
    public partial class PostBuildReportSection : UserControl
    {
        public static readonly DependencyProperty DefaultTextProperty = DependencyProperty.Register(
            nameof(DefaultText), typeof(string), typeof(PostBuildReportSection), new PropertyMetadata("Section Title"));
        public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(
            nameof(ErrorText), typeof(string), typeof(PostBuildReportSection), new PropertyMetadata("Section Title"));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
            nameof(Status), typeof(PostBuildReportSectionStatus), typeof(PostBuildReportSection),
            new PropertyMetadata(PostBuildReportSectionStatus.OK));

        public string DefaultText
        {
            get => (string)GetValue(DefaultTextProperty);
            set => SetValue(DefaultTextProperty, value);
        }

        public string ErrorText
        {
            get => (string)GetValue(ErrorTextProperty);
            set => SetValue(ErrorTextProperty, value);
        }

        public PostBuildReportSectionStatus Status
        {
            get => (PostBuildReportSectionStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public PostBuildReportSection()
        {
            InitializeComponent();
        }
    }
}
