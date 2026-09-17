using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using dagangOnline.Mobile.Helpers;
using dagangOnline.Mobile.Models;
using dagangOnline.Mobile.Services;

namespace dagangOnline.Mobile.ViewModels;

public class HumanAgentViewModel : BaseViewModel
{
    private readonly IHumanAgentService _agentService;
    private string _currentInput = string.Empty;
    private bool _isTyping;
    private bool _isEscalated;
    private string _escalationText = string.Empty;
    private ChatMessageModel? _pendingFeedbackMessage;
    private bool _showFeedbackModal;

    public Guid SessionId { get; set; } = Guid.NewGuid();
    public ObservableCollection<ChatMessageModel> Messages { get; } = new();

    public string CurrentInput
    {
        get => _currentInput;
        set => SetProperty(ref _currentInput, value);
    }

    public bool IsTyping
    {
        get => _isTyping;
        set => SetProperty(ref _isTyping, value);
    }

    public bool IsEscalated
    {
        get => _isEscalated;
        set => SetProperty(ref _isEscalated, value);
    }

    public string EscalationText
    {
        get => _escalationText;
        set => SetProperty(ref _escalationText, value);
    }

    public bool ShowFeedbackModal
    {
        get => _showFeedbackModal;
        set => SetProperty(ref _showFeedbackModal, value);
    }

    public ICommand SendCommand { get; }
    public ICommand LikeCommand { get; }
    public ICommand OpenUnlikeModalCommand { get; }
    public ICommand SelectUnlikeReasonCommand { get; }
    public ICommand CloseFeedbackModalCommand { get; }

    public HumanAgentViewModel(IHumanAgentService? agentService = null)
    {
        _agentService = agentService ?? new HumanAgentService();
        Title = "Human Agent CS Support";

        SendCommand = new RelayCommand(async () => await SendMessageAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(CurrentInput));
        LikeCommand = new RelayCommand(async (param) =>
        {
            if (param is ChatMessageModel msg)
            {
                await SubmitLikeAsync(msg);
            }
        });

        OpenUnlikeModalCommand = new RelayCommand((param) =>
        {
            if (param is ChatMessageModel msg)
            {
                _pendingFeedbackMessage = msg;
                ShowFeedbackModal = true;
            }
        });

        SelectUnlikeReasonCommand = new RelayCommand(async (param) =>
        {
            if (param is string reason && _pendingFeedbackMessage != null)
            {
                await SubmitUnlikeAsync(_pendingFeedbackMessage, reason);
                ShowFeedbackModal = false;
                _pendingFeedbackMessage = null;
            }
        });

        CloseFeedbackModalCommand = new RelayCommand(() =>
        {
            ShowFeedbackModal = false;
            _pendingFeedbackMessage = null;
        });

        // Add welcome message
        Messages.Add(new ChatMessageModel
        {
            Content = "Halo! Selamat datang di layanan bantuan dagangOnline. Ada yang bisa kami bantu seputar produk, UMKM, pengiriman, atau kemitraan hari ini? (Mendukung Bahasa Indonesia, English, Basa Jawa, & Basa Sunda)",
            IsUser = false,
            Timestamp = DateTime.UtcNow,
            EvidenceCount = 1,
            GroundingState = "Grounded"
        });
    }

    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput)) return;

        var textToSend = CurrentInput.Trim();
        CurrentInput = string.Empty;

        var userMsg = new ChatMessageModel
        {
            Content = textToSend,
            IsUser = true,
            Timestamp = DateTime.UtcNow
        };
        Messages.Add(userMsg);

        IsBusy = true;
        IsTyping = true;

        var (aiResponse, error) = await _agentService.SendMessageAsync(SessionId, textToSend);

        IsTyping = false;
        IsBusy = false;

        if (aiResponse != null)
        {
            Messages.Add(aiResponse);

            if (aiResponse.IsEscalated)
            {
                IsEscalated = true;
                EscalationText = "⚠️ Permintaan telah diteruskan ke Human Agent. Operator akan segera bergabung.";
            }
        }
        else
        {
            Messages.Add(new ChatMessageModel
            {
                Content = error ?? "Terjadi kesalahan jaringan. Silakan coba lagi.",
                IsUser = false,
                Timestamp = DateTime.UtcNow,
                IsEscalated = true
            });
        }
    }

    private async Task SubmitLikeAsync(ChatMessageModel message)
    {
        message.FeedbackGiven = "Like";
        OnPropertyChanged(nameof(Messages));
        await _agentService.SubmitFeedbackAsync(SessionId, message.Id, "Like", null, null);
    }

    private async Task SubmitUnlikeAsync(ChatMessageModel message, string reason)
    {
        message.FeedbackGiven = "Unlike";
        message.UnlikeReason = reason;
        OnPropertyChanged(nameof(Messages));
        await _agentService.SubmitFeedbackAsync(SessionId, message.Id, "Unlike", reason, null);
    }
}
