namespace Quadra.App.Controls;

public sealed class ZoomStateChangedEventArgs : EventArgs
{
    public ZoomStateChangedEventArgs(bool isZoomed)
    {
        IsZoomed = isZoomed;
    }

    public bool IsZoomed { get; }
}

public class ZoomableImage : ContentView
{
    private const double MinimumScale = 1;
    private const double DoubleTapScale = 2.5;
    private const double ZoomTolerance = 0.01;

    private readonly Image _image;
    private readonly PanGestureRecognizer _panGesture;

    private double _currentScale = MinimumScale;

    private double _currentTranslationX;
    private double _currentTranslationY;

    private double _panStartTranslationX;
    private double _panStartTranslationY;

    private bool _panGestureAttached;
    private bool _zoomStateNotified;

    public event EventHandler<ZoomStateChangedEventArgs>?
        ZoomStateChanged;

    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(
            nameof(Source),
            typeof(ImageSource),
            typeof(ZoomableImage),
            default(ImageSource),
            propertyChanged: OnSourceChanged);

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public ZoomableImage()
    {
        BackgroundColor = Colors.Black;

        _image = new Image
        {
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            AnchorX = 0.5,
            AnchorY = 0.5
        };

        _panGesture = new PanGestureRecognizer
        {
            TouchPoints = 1
        };

        _panGesture.PanUpdated += OnPanUpdated;

        var doubleTapGesture = new TapGestureRecognizer
        {
            NumberOfTapsRequired = 2
        };

        doubleTapGesture.Tapped += OnDoubleTapped;

        /*
         * O gesto de pan não é adicionado inicialmente.
         * Em escala normal, o movimento horizontal pertence
         * somente ao CarouselView.
         */
        GestureRecognizers.Add(doubleTapGesture);

        Content = _image;
    }

    public void ResetZoom()
    {
        ResetZoom(notifyZoomState: true);
    }

    private static void OnSourceChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var control = (ZoomableImage)bindable;

        control._image.Source =
            (ImageSource?)newValue;

        /*
         * O CarouselView pode reutilizar a célula.
         * Por isso, toda nova imagem começa em escala normal.
         */
        control.ResetZoom(
            notifyZoomState: false);
    }

    private void OnDoubleTapped(
        object? sender,
        TappedEventArgs e)
    {
        if (IsZoomed())
        {
            ResetZoom();
            return;
        }

        _currentScale = DoubleTapScale;

        _image.Scale = _currentScale;

        ResetTranslations();
        AttachPanGesture();
        ApplyTranslationLimits();

        NotifyZoomState(
            isZoomed: true);
    }

    private void OnPanUpdated(
        object? sender,
        PanUpdatedEventArgs e)
    {
        if (!IsZoomed())
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartTranslationX =
                    _currentTranslationX;

                _panStartTranslationY =
                    _currentTranslationY;
                break;

            case GestureStatus.Running:
                _currentTranslationX =
                    _panStartTranslationX +
                    e.TotalX;

                _currentTranslationY =
                    _panStartTranslationY +
                    e.TotalY;

                ApplyTranslationLimits();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                ApplyTranslationLimits();
                break;
        }
    }

    private bool IsZoomed()
    {
        return _currentScale >
               MinimumScale + ZoomTolerance;
    }

    private void ResetZoom(
        bool notifyZoomState)
    {
        ResetToMinimumScale();

        if (notifyZoomState)
        {
            NotifyZoomState(
                isZoomed: false);
        }
        else
        {
            _zoomStateNotified = false;
        }
    }

    private void ResetToMinimumScale()
    {
        _currentScale = MinimumScale;

        _image.Scale = MinimumScale;

        ResetTranslations();
        DetachPanGesture();
    }

    private void ResetTranslations()
    {
        _currentTranslationX = 0;
        _currentTranslationY = 0;

        _panStartTranslationX = 0;
        _panStartTranslationY = 0;

        _image.TranslationX = 0;
        _image.TranslationY = 0;
    }

    private void ApplyTranslationLimits()
    {
        if (!IsZoomed() ||
            Width <= 0 ||
            Height <= 0)
        {
            ResetTranslations();
            return;
        }

        /*
         * Como a imagem é ampliada em torno do centro,
         * metade do tamanho adicional fica disponível
         * para deslocamento em cada direção.
         */
        var horizontalOverflow =
            Width *
            (_currentScale - MinimumScale) /
            2;

        var verticalOverflow =
            Height *
            (_currentScale - MinimumScale) /
            2;

        _currentTranslationX = Math.Clamp(
            _currentTranslationX,
            -horizontalOverflow,
            horizontalOverflow);

        _currentTranslationY = Math.Clamp(
            _currentTranslationY,
            -verticalOverflow,
            verticalOverflow);

        _image.TranslationX =
            _currentTranslationX;

        _image.TranslationY =
            _currentTranslationY;
    }

    private void AttachPanGesture()
    {
        if (_panGestureAttached)
            return;

        GestureRecognizers.Add(
            _panGesture);

        _panGestureAttached = true;
    }

    private void DetachPanGesture()
    {
        if (!_panGestureAttached)
            return;

        GestureRecognizers.Remove(
            _panGesture);

        _panGestureAttached = false;
    }

    private void NotifyZoomState(
        bool isZoomed)
    {
        if (_zoomStateNotified == isZoomed)
            return;

        _zoomStateNotified = isZoomed;

        ZoomStateChanged?.Invoke(
            this,
            new ZoomStateChangedEventArgs(
                isZoomed));
    }
}