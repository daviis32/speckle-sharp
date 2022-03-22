using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Views;
using DesktopUI2.Views.Windows.Dialogs;
using Material.Dialog;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class HomeViewModel : ReactiveObject, IRoutableViewModel
  {
    //Instance of this HomeViewModel, so that the SavedStreams are kept in memory and not disposed on navigation
    public static HomeViewModel Instance { get; private set; }
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = "home";

    private ConnectorBindings Bindings;

    #region bindings
    public ReactiveCommand<string, Unit> RemoveSavedStreamCommand { get; }

    private bool _showProgress;
    public bool InProgress
    {
      get => _showProgress;
      private set => this.RaiseAndSetIfChanged(ref _showProgress, value);
    }

    private List<StreamAccountWrapper> _streams;
    public List<StreamAccountWrapper> Streams
    {
      get => _streams;
      private set => this.RaiseAndSetIfChanged(ref _streams, value);
    }

    public bool HasSavedStreams => SavedStreams != null && SavedStreams.Any();

    private string _searchQuery;

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        SearchStreams().ConfigureAwait(false);
      }
    }

    //public Stream SelectedStream
    //{
    //  set
    //  {
    //    if (value != null)
    //    {
    //      var streamState = new StreamState(SelectedAccount, value);
    //      OpenStream(value, streamState);
    //    }
    //  }
    //}

    private ObservableCollection<SavedStreamViewModel> _savedStreams = new ObservableCollection<SavedStreamViewModel>();
    public ObservableCollection<SavedStreamViewModel> SavedStreams
    {
      get => _savedStreams;
      set
      {
        this.RaiseAndSetIfChanged(ref _savedStreams, value);
        this.RaisePropertyChanged("HasSavedStreams");
      }
    }

    private List<Account> _accounts;
    public List<Account> Accounts
    {
      get => _accounts;
      private set
      {
        this.RaiseAndSetIfChanged(ref _accounts, value);
        this.RaisePropertyChanged("HasMultipleAccounts");
      }
    }

    public bool HasMultipleAccounts
    {
      get => Accounts.Count > 1;

    }

    #endregion

    public HomeViewModel(IScreen screen)
    {
      Instance = this;
      HostScreen = screen;
      RemoveSavedStreamCommand = ReactiveCommand.Create<string>(RemoveSavedStream);

      SavedStreams.CollectionChanged += SavedStreams_CollectionChanged;

      Bindings = Locator.Current.GetService<ConnectorBindings>();
      this.RaisePropertyChanged("SavedStreams");
      Init();
    }

    /// <summary>
    /// This get usually triggered on file open or view activated
    /// </summary>
    /// <param name="streams"></param>
    internal void UpdateSavedStreams(List<StreamState> streams)
    {
      SavedStreams.CollectionChanged -= SavedStreams_CollectionChanged;
      SavedStreams = new ObservableCollection<SavedStreamViewModel>();
      streams.ForEach(x => SavedStreams.Add(new SavedStreamViewModel(x, HostScreen, RemoveSavedStreamCommand)));
      this.RaisePropertyChanged("HasSavedStreams");
      SavedStreams.CollectionChanged += SavedStreams_CollectionChanged;
    }

    //write changes to file every time they happen
    //this is because if there is an active document change we need to swap saved streams and restore them later
    //even if the doc has not been saved
    private void SavedStreams_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      WriteStreamsToFile();
    }

    internal void WriteStreamsToFile()
    {
      Bindings.WriteStreamsToFile(SavedStreams.Select(x => x.StreamState).ToList());
    }

    internal void AddSavedStream(StreamState streamState, bool send = false, bool receive = false)
    {
      //saved stream has been edited
      var savedState = SavedStreams.FirstOrDefault(x => x.StreamState.Id == streamState.Id);
      if (savedState != null)
      {
        savedState.StreamState = streamState;
        WriteStreamsToFile();
      }
      //it's a new saved stream
      else
      {
        savedState = new SavedStreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand);
        SavedStreams.Add(savedState);
      }

      this.RaisePropertyChanged("HasSavedStreams");

      //for save&send and save&receive
      if (send)
        savedState.SendCommand();

      if (receive)
        savedState.ReceiveCommand();
    }

    private async Task GetStreams()
    {

      InProgress = true;

      Streams = new List<StreamAccountWrapper>();

      foreach (var account in Accounts)
      {
        try
        {
          var client = new Client(account);
          Streams.AddRange((await client.StreamsGet()).Select(x => new StreamAccountWrapper(x, account)));
        }
        catch (Exception e)
        {
          Dialogs.ShowDialog($"Could not get streams for {account.userInfo.email} on {account.serverInfo.url}.", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
        }
      }
      Streams = Streams.OrderByDescending(x => DateTime.Parse(x.Stream.updatedAt)).ToList();

      InProgress = false;

    }

    private async Task SearchStreams()
    {
      if (SearchQuery == "")
      {
        GetStreams().ConfigureAwait(false);
        return;
      }
      if (SearchQuery.Length <= 2)
        return;
      InProgress = true;

      Streams = new List<StreamAccountWrapper>();

      foreach (var account in Accounts)
      {
        try
        {
          var client = new Client(account);
          Streams.AddRange((await client.StreamSearch(SearchQuery)).Select(x => new StreamAccountWrapper(x, account)));
        }
        catch (Exception e)
        {

        }
      }

      Streams = Streams.OrderByDescending(x => DateTime.Parse(x.Stream.updatedAt)).ToList();

      InProgress = false;

    }

    internal void Init()
    {
      Accounts = AccountManager.GetAccounts().ToList();
      GetStreams();

      if (!Accounts.Any())
      {
        ShowNoAccountPopup();
        return;
      }
    }

    private void RemoveSavedStream(string id)
    {
      var s = SavedStreams.FirstOrDefault(x => x.StreamState.Id == id);
      if (s != null)
      {
        SavedStreams.Remove(s);
        Tracker.TrackPageview("stream", "remove");
        Analytics.TrackEvent(s.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Remove" } });
      }

      this.RaisePropertyChanged("HasSavedStreams");
    }

    private async void ShowNoAccountPopup()
    {
      var btn = new Button()
      {
        Content = "Launch Manager...",
        Margin = new Avalonia.Thickness(20)
      };
      btn.Click += LaunchManagerBtnClick;
      await DialogHelper.CreateCustomDialog(new CustomDialogBuilderParams()
      {
        ContentHeader = "No account found!",
        WindowTitle = "No account found!",
        DialogHeaderIcon = Material.Dialog.Icons.DialogIconKind.Error,
        StartupLocation = WindowStartupLocation.CenterOwner,
        NegativeResult = new DialogResult("retry"),
        Borderless = true,
        MaxWidth = MainWindow.Instance.Width - 40,
        DialogButtons = new DialogButton[]
          {
            new DialogButton
            {
              Content = "TRY AGAIN",
              Result = "retry"
            },

          },
        Content = btn
      }).ShowDialog(MainWindow.Instance);

      Init();
    }

    private void LaunchManagerBtnClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        Process.Start(@"/Applications/SpeckleManager.app");
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        Process.Start(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "speckle-manager", "SpeckleManager.exe"));
      }

    }

    public void ClearSearchCommand()
    {
      SearchQuery = "";
    }
    public void ViewOnlineCommand(object parameter)
    {
      var streamAcc = parameter as StreamAccountWrapper;
      Process.Start(new ProcessStartInfo($"{streamAcc.Account.serverInfo.url.TrimEnd('/')}/streams/{streamAcc.Stream.id}") { UseShellExecute = true });
      Tracker.TrackPageview(Tracker.STREAM_VIEW);
      Analytics.TrackEvent(streamAcc.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream View" } });
    }

    public void SendCommand(object parameter)
    {
      var streamAcc = parameter as StreamAccountWrapper;
      var streamState = new StreamState(streamAcc);
      OpenStream(streamState);
    }

    public void ReceiveCommand(object parameter)
    {
      var streamAcc = parameter as StreamAccountWrapper;
      var streamState = new StreamState(streamAcc) { IsReceiver = true };
      OpenStream(streamState);
    }

    public async void NewStreamCommand()
    {
      var dialog = new NewStreamDialog(Accounts);
      dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      await dialog.ShowDialog(MainWindow.Instance);



      if (dialog.Create)
      {
        try
        {
          var client = new Client(dialog.Account);
          var streamId = await client.StreamCreate(new StreamCreateInput { description = dialog.Description, name = dialog.StreamName, isPublic = dialog.IsPublic });
          var stream = await client.StreamGet(streamId);
          var streamState = new StreamState(dialog.Account, stream);

          OpenStream(streamState);

          Tracker.TrackPageview(Tracker.STREAM_CREATE);
          Analytics.TrackEvent(dialog.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Create" } });

          GetStreams().ConfigureAwait(false); //update streams
        }
        catch (Exception e)
        {
          Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
        }
      }
    }

    [DependsOn(nameof(InProgress))]
    public bool CanNewStreamCommand(object parameter)
    {
      return !InProgress;
    }

    public async void AddFromUrlCommand()
    {
      var clipboard = await Avalonia.Application.Current.Clipboard.GetTextAsync();

      Uri uri;
      string defaultText = "";
      if (Uri.TryCreate(clipboard, UriKind.Absolute, out uri))
        defaultText = clipboard;

      var dialog = new AddFromUrlDialog();
      dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      await dialog.ShowDialog(MainWindow.Instance);


      if (dialog.Add)
      {
        try
        {
          var sw = new StreamWrapper(dialog.Url);
          var account = await sw.GetAccount();
          var client = new Client(account);
          var stream = await client.StreamGet(sw.StreamId);
          var streamState = new StreamState(account, stream);

          OpenStream(streamState);

          Tracker.TrackPageview("stream", "add-from-url");
          Analytics.TrackEvent(account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Add From URL" } });
        }
        catch (Exception e)
        {
          Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
        }
      }
    }

    private Tuple<bool, string> ValidateUrl(string url)
    {
      Uri uri;
      try
      {
        if (Uri.TryCreate(url, UriKind.Absolute, out uri))
        {
          var sw = new StreamWrapper(url);
        }
        else return new Tuple<bool, string>(false, "URL is not valid.");
      }
      catch { return new Tuple<bool, string>(false, "URL is not a Stream."); }

      return new Tuple<bool, string>(true, "");
    }

    private Tuple<bool, string> ValidateName(string name)
    {
      if (string.IsNullOrEmpty(name))
        return new Tuple<bool, string>(false, "Streams need a name too!");

      if (name.Trim().Length < 3)
        return new Tuple<bool, string>(false, "Name is too short.");

      return new Tuple<bool, string>(true, "");
    }

    [DependsOn(nameof(InProgress))]
    public bool CanAddFromUrlCommand(object parameter)
    {
      return !InProgress;
    }

    private void OpenStream(StreamState streamState)
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(new StreamEditViewModel(HostScreen, streamState));
    }

  }
}