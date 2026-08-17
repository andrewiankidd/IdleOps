namespace IdleOps.Shared.Ocr;

/// <summary>A single OCR-recognized word and its bounding box, in image pixels.</summary>
public sealed record RecognizedWord(string Text, int X, int Y, int Width, int Height);
