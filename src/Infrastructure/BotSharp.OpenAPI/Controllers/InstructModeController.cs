using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class InstructModeController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InstructModeController> _logger;

    public InstructModeController(IServiceProvider services, ILogger<InstructModeController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpPost("/instruct/{agentId}")]
    public async Task<InstructResult> InstructCompletion([FromRoute] string agentId, [FromBody] InstructMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        state.SetState("provider", input.Provider, source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External)
            .SetState("instruction", input.Instruction, source: StateSource.External)
            .SetState("input_text", input.Text,source: StateSource.External);

        var instructor = _services.GetRequiredService<IInstructService>();
        var result = await instructor.Execute(agentId,
            new RoleDialogModel(AgentRole.User, input.Text),
            templateName: input.Template,
            instruction: input.Instruction);

        result.States = state.GetStates();

        return result; 
    }

    [HttpPost("/instruct/text-completion")]
    public async Task<string> TextCompletion([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        state.SetState("provider", input.Provider ?? "azure-openai", source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External);

        var textCompletion = CompletionProvider.GetTextCompletion(_services);
        return await textCompletion.GetCompletion(input.Text, Guid.Empty.ToString(), Guid.NewGuid().ToString());
    }

    #region Chat
    [HttpPost("/instruct/chat-completion")]
    public async Task<string> ChatCompletion([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        state.SetState("provider", input.Provider, source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External);

        var textCompletion = CompletionProvider.GetChatCompletion(_services);
        var message = await textCompletion.GetChatCompletions(new Agent()
        {
            Id = Guid.Empty.ToString(),
        }, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, input.Text)
        });
        return message.Content;
    }
    #endregion

    #region Read image
    [HttpPost("/instruct/multi-modal")]
    public async Task<string> MultiModalCompletion([FromBody] MultiModalRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadImages(input.Provider, input.Model, input.Text, input.Files);
            return content;
        }
        catch (Exception ex)
        {
            var error = $"Error in analyzing files. {ex.Message}";
            _logger.LogError(error);
            return error;
        }
    }
    #endregion

    #region Generate image
    [HttpPost("/instruct/image-generation")]
    public async Task<ImageGenerationViewModel> ImageGeneration([FromBody] ImageGenerationRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var message = await fileInstruct.GenerateImage(input.Provider, input.Model, input.Text);
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image generation. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Edit image
    [HttpPost("/instruct/image-variation")]
    public async Task<ImageGenerationViewModel> ImageVariation([FromBody] ImageVariationRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (input.File == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find an image!" };
            }

            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var message = await fileInstruct.VaryImage(input.Provider, input.Model, input.File);
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image variation. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-edit")]
    public async Task<ImageGenerationViewModel> ImageEdit([FromBody] ImageEditRequest input)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (input.File == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find a valid image file!" };
            }
            var message = await fileInstruct.EditImage(input.Provider, input.Model, input.Text, input.File);
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image edit. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-mask-edit")]
    public async Task<ImageGenerationViewModel> ImageMaskEdit([FromBody] ImageMaskEditRequest input)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var image = input.File;
            var mask = input.Mask;
            if (image == null || mask == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find a valid image or mask!" };
            }
            var message = await fileInstruct.EditImage(input.Provider, input.Model, input.Text, image, mask);
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image mask edit. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Pdf
    [HttpPost("/instruct/pdf-completion")]
    public async Task<PdfCompletionViewModel> PdfCompletion([FromBody] MultiModalRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var viewModel = new PdfCompletionViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadPdf(input.Provider, input.Model, input.ModelId, input.Text, input.Files);
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in pdf completion. {ex.Message}";
            _logger.LogError(error);
            viewModel.Message = error;
            return viewModel;
        }
    }
    #endregion

    #region Audio
    [HttpPost("/instruct/audio-completion")]
    public async Task<AudioCompletionViewModel> AudioCompletion([FromBody] AudioCompletionRequest input)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var viewModel = new AudioCompletionViewModel();

        try
        {
            var audio = input.File;
            if (audio == null)
            {
                return new AudioCompletionViewModel { Message = "Error! Cannot find a valid audio file!" };
            }
            var content = await fileInstruct.ReadAudio(input.Provider, input.Model, audio);
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in audio completion. {ex.Message}";
            _logger.LogError(error);
            viewModel.Message = error;
            return viewModel;
        }
    }
    #endregion
}
