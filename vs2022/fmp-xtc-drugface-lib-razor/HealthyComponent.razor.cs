
using Microsoft.AspNetCore.Components;
using XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.DrugFace.LIB.Proto;
using XTC.FMP.MOD.DrugFace.LIB.Bridge;
using XTC.FMP.MOD.DrugFace.LIB.MVCS;

namespace XTC.FMP.MOD.DrugFace.LIB.Razor
{
    public partial class HealthyComponent
    {
        public class HealthyUiBridge : IHealthyUiBridge
        {

            public HealthyUiBridge(HealthyComponent _razor)
            {
                razor_ = _razor;
            }

            public void Alert(string _code, string _message, object? _context)
            {
                if (null == razor_.messageService_)
                    return;
                Task.Run(async () =>
                {
                    await razor_.messageService_.Error(_message);
                }); 
            }


            public void RefreshEcho(IDTO _dto, object? _context)
            {
                var dto = _dto as HealthyEchoResponseDTO;
                razor_.__debugEcho = dto?.Value.ToString();
            }


            private HealthyComponent razor_;
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        private async Task __debugClick()
        {
            var bridge = (getFacade()?.getViewBridge() as IHealthyViewBridge);
            if (null == bridge)
            {
                logger_?.Error("bridge is null");
                return;
            }

            var reqEcho = new HealthyEchoRequest();
            var dtoEcho = new HealthyEchoRequestDTO(reqEcho);
            logger_?.Trace("invoke OnEchoSubmit");
            await bridge.OnEchoSubmit(dtoEcho, null);

        }


        private string? __debugEcho;

    }
}
