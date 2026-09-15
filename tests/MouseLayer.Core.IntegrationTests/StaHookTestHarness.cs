using System.Windows.Forms;
using MouseLayer.Core.Profiles;

namespace MouseLayer.Core.IntegrationTests;

/// <summary>
/// WH_MOUSE_LL / WH_KEYBOARD_LL hooks only deliver callbacks on the thread that installed
/// them, and only while that thread is pumping Win32 messages. This harness runs the engine
/// and a WinForms message loop on a dedicated STA thread, injects input via a Timer tick
/// (so it happens *after* the loop is pumping), then reads back what the verification hook saw.
/// </summary>
internal sealed class StaHookTestHarness
{
    private readonly MouseLayerConfig _config;
    private readonly ushort _watchedVirtualKey;
    private readonly Action _triggerInput;

    public StaHookTestHarness(MouseLayerConfig config, ushort watchedVirtualKey, Action triggerInput)
    {
        _config = config;
        _watchedVirtualKey = watchedVirtualKey;
        _triggerInput = triggerInput;
    }

    public (bool keyDownObserved, bool keyUpObserved) Run(TimeSpan timeout)
    {
        var keyDownObserved = false;
        var keyUpObserved = false;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                using var engine = new MouseLayerEngine(_config);
                using var keyboardHook = new KeyboardVerificationHook(_watchedVirtualKey);
                engine.Start();

                using var context = new HarnessApplicationContext(_triggerInput, () =>
                {
                    keyDownObserved = keyboardHook.KeyDownObserved;
                    keyUpObserved = keyboardHook.KeyUpObserved;
                });

                Application.Run(context);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(timeout))
            throw new TimeoutException("Integration test harness did not complete within the timeout.");
        if (failure != null)
            throw failure;

        return (keyDownObserved, keyUpObserved);
    }

    private sealed class HarnessApplicationContext : ApplicationContext
    {
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 100 };
        private readonly Action _triggerInput;
        private readonly Action _collectResults;
        private int _tick;

        public HarnessApplicationContext(Action triggerInput, Action collectResults)
        {
            _triggerInput = triggerInput;
            _collectResults = collectResults;
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _tick++;
            if (_tick == 2)
            {
                // Let the hooks finish registering before the first injected input.
                _triggerInput();
            }
            else if (_tick >= 6)
            {
                // Give the hook chain time to process the input and inject its replacement.
                _timer.Stop();
                _collectResults();
                ExitThread();
            }
        }
    }
}
