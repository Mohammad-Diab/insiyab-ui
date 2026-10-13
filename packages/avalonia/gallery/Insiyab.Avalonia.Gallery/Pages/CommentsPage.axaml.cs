using System.Windows.Input;
using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("comments", "التعليقات والرسائل")]
public partial class CommentsPage : UserControl
{
    public CommentsPage()
    {
        InitializeComponent();
        MenuCopy.Command = MenuComment.CopyCommand;
        Comment.SetSubmitCommand(ReplyBox, new Send(Post));
        ReplySend.Click += (_, _) => Post(ReplyBox.Text ?? "");
    }

    private void Post(string text)
    {
        if (text.Trim().Length == 0) return;
        var reply = new Comment { Avatar = "أ", Author = "أنت", Time = "الآن", Content = text.Trim() };
        reply.Classes.Add("own");
        ReplyThread.Children.Add(reply);
        ReplyBox.Text = "";
    }

    private sealed class Send(Action<string> run) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run(parameter as string ?? "");
    }
}
