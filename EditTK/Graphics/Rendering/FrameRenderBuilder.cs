using EditTK.Graphics.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.Graphics.Rendering
{
    //TODO

    public class FrameRenderBuilder
    {
        internal record DeviceTextureInfo(string Name, PixelFormat Format);

        internal record FrameBufferInfo(RenderTextureRef? requestedDepthTex, RenderTextureRef[] requestedColorTexs);

        record TextureUsage(int TextureSlot, int Start, int End);

        private static bool Collides(TextureUsage usage, TextureUsage other) =>
            (usage.Start <= other.End) &&
            (other.Start <= usage.End);




        private readonly List<TextureUsage> _renderTextureUsages = new();
        private readonly List<RenderTextureRef> _renderTextureSlots = new();
        private readonly List<IPixelReader> _pixelReaderSlots = new();
        private readonly List<FrameRenderInstruction> _instructions = new();
        private readonly List<FrameBufferInfo> _frameBufferSlots = new();

        private readonly List<CompositionResourceSetup> _compResourceSlots = new();


        internal IReadOnlyList<RenderTextureRef> RenderTextureSlots => _renderTextureSlots;
        internal IReadOnlyList<IPixelReader> PixelReaderSlots => _pixelReaderSlots;
        internal IReadOnlyList<FrameRenderInstruction> Instructions => _instructions;
        internal IReadOnlyList<CompositionResourceSetup> CompResourceSetups => _compResourceSlots;

        public int FramebufferSlotCount => _frameBufferSlots.Count;


        internal List<(DeviceTextureInfo textureInfo, int[] slots)> GetRenderTextureSlotGroups()
        {
            List<(DeviceTextureInfo textureInfo, int[] slots)> textureSlotGroups = new();



            //GOAL: find the least amount of textures needed for all texture usages in one frame render process

            var textureUsagePerFormat = _renderTextureUsages.GroupBy(x => _renderTextureSlots[x.TextureSlot].Format);

            foreach (var usageGroup in textureUsagePerFormat)
            {
                Dictionary<int, List<TextureUsage>> _optimizedUsagesPerTexture = new();


                int Remap(TextureUsage usage)
                {
                    foreach (var (texture, usages) in _optimizedUsagesPerTexture)
                    {
                        bool hasCollision = false;

                        foreach (TextureUsage other in usages)
                        {
                            if (Collides(usage, other))
                            {
                                hasCollision = true;
                                break;
                            }
                        }

                        if (!hasCollision)
                        {
                            return texture;
                        }
                    }

                    return usage.TextureSlot;
                }




                foreach (TextureUsage usage in usageGroup)
                {
                    var remappedTextureSlot = Remap(usage);

                    if (!_optimizedUsagesPerTexture.TryGetValue(remappedTextureSlot, out List<TextureUsage>? usagesForThisTexture))
                        _optimizedUsagesPerTexture[remappedTextureSlot] = usagesForThisTexture = new List<TextureUsage>();

                    usagesForThisTexture.Add(usage); //should never be read from after this anyways but better safe than sorry
                }

                foreach (var (_, usageList) in _optimizedUsagesPerTexture)
                {
                    int[] slots = usageList.Select(x => x.TextureSlot).Distinct().ToArray();

                    textureSlotGroups.Add((
                        new DeviceTextureInfo(
                            string.Join('/', slots.Select(slot => _renderTextureSlots[slot].Name)),
                            usageGroup.Key //contains the texture format all these textures have in common
                        ),
                        slots
                    ));
                }
            }

            return textureSlotGroups;
        }

        internal (FrameBufferInfo info, int[] framebufferSlots)[] GetFramebufferSlotGroups(Dictionary<RenderTextureRef, RenderTextureRef> slotRemapping)
        {
            var nullTex = new RenderTextureRef();

            List<(FrameBufferInfo info, List<int> framebufferSlots)> uniqueGroups = new();

            int fbSlotIndex = 0;

            foreach (var (requestedDepthTex, requestedColorTexs) in _frameBufferSlots)
            {


                RenderTextureRef? requestedDepthSlot = requestedDepthTex == null ? null : slotRemapping[requestedDepthTex];

                List<RenderTextureRef> requestedColorSlots = requestedColorTexs.Select(x => slotRemapping[x]).ToList();




                bool hasDuplicate = false;

                foreach (var ((requestedDepthSlot_, requestedColorSlots_), fbSlots) in uniqueGroups)
                {
                    bool isDuplicate = true;

                    if (requestedDepthSlot_ != requestedDepthSlot)
                    {
                        isDuplicate = false;
                    }
                    else
                    {
                        for (int i = 0; i < requestedColorSlots_.Length; i++)
                        {
                            if (requestedColorSlots_[i] != requestedColorSlots[i])
                            {
                                isDuplicate = false;
                                break;
                            }
                        }
                    }

                    if (isDuplicate)
                    {
                        hasDuplicate = true;

                        fbSlots.Add(fbSlotIndex);

                        break;
                    }
                }

                if (!hasDuplicate)
                {
                    uniqueGroups.Add((new FrameBufferInfo(requestedDepthSlot, requestedColorSlots.ToArray()), new List<int>()));
                }

                fbSlotIndex++;
            }

            return uniqueGroups.Select(x => (x.info, x.framebufferSlots.ToArray())).ToArray();
        }


        private static void EnsureCompatibleType<T>(PixelFormat pixelFormat) where T : struct
        {
            Type compontentType;
            int compontentCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)] void uint8(int c) { compontentCount = c; compontentType = typeof(byte); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] void sint8(int c) { compontentCount = c; compontentType = typeof(sbyte); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] void uint16(int c) { compontentCount = c; compontentType = typeof(ushort); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] void sint16(int c) { compontentCount = c; compontentType = typeof(short); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] void uint32(int c) { compontentCount = c; compontentType = typeof(uint); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] void sint32(int c) { compontentCount = c; compontentType = typeof(int); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] void float16(int c) { compontentCount = c; compontentType = typeof(Half); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] void float32(int c) { compontentCount = c; compontentType = typeof(float); }

            switch (pixelFormat)
            {
                case PixelFormat.R8_UNorm:
                case PixelFormat.R8_UInt:
                    uint8(1); break;
                case PixelFormat.R8_SNorm:
                case PixelFormat.R8_SInt:
                    sint8(1); break;
                case PixelFormat.R8_G8_UNorm:
                case PixelFormat.R8_G8_UInt:
                    uint8(2); break;
                case PixelFormat.R8_G8_SNorm:
                case PixelFormat.R8_G8_SInt:
                    sint8(2); break;
                case PixelFormat.R8_G8_B8_A8_UNorm:
                case PixelFormat.B8_G8_R8_A8_UNorm:
                case PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                case PixelFormat.B8_G8_R8_A8_UNorm_SRgb:
                case PixelFormat.R8_G8_B8_A8_UInt:
                    uint8(4); break;
                case PixelFormat.R8_G8_B8_A8_SInt:
                case PixelFormat.R8_G8_B8_A8_SNorm:
                    sint8(4); break;

                case PixelFormat.R16_UNorm:
                case PixelFormat.R16_UInt:
                    uint16(1); break;
                case PixelFormat.R16_SNorm:
                case PixelFormat.R16_SInt:
                    sint16(1); break;
                case PixelFormat.R16_G16_UNorm:
                case PixelFormat.R16_G16_UInt:
                    uint16(2); break;
                case PixelFormat.R16_G16_SNorm:
                case PixelFormat.R16_G16_SInt:
                    sint16(2); break;
                case PixelFormat.R16_G16_B16_A16_UNorm:
                case PixelFormat.R16_G16_B16_A16_UInt:
                    uint16(4); break;
                case PixelFormat.R16_G16_B16_A16_SNorm:
                case PixelFormat.R16_G16_B16_A16_SInt:
                    sint16(4); break;

                case PixelFormat.R32_UInt:
                    uint32(1); break;
                case PixelFormat.R32_SInt:
                    sint32(1); break;
                case PixelFormat.R32_G32_UInt:
                    uint32(2); break;
                case PixelFormat.R32_G32_SInt:
                    sint32(2); break;
                case PixelFormat.R32_G32_B32_A32_UInt:
                    uint32(4); break;
                case PixelFormat.R32_G32_B32_A32_SInt:
                    sint32(4); break;


                case PixelFormat.R16_Float:
                    float16(1); break;
                case PixelFormat.R16_G16_Float:
                    float16(2); break;
                case PixelFormat.R16_G16_B16_A16_Float:
                    float16(4); break;

                case PixelFormat.R32_Float:
                    float32(1); break;
                case PixelFormat.R32_G32_Float:
                    float32(2); break;
                case PixelFormat.R32_G32_B32_A32_Float:
                    float32(4); break;


                default:
                    throw new ArgumentException($"Unsupported {nameof(PixelFormat)} {pixelFormat} for direct assignment");
            }

            if (compontentCount == 1 && compontentType == typeof(T))
                return;


            var fieldInfos = typeof(T).GetFields();

            if (fieldInfos.Length != compontentCount)
                throw new ArgumentException($"The number of fields in {typeof(T).Name} ({fieldInfos.Length}) " +
                    $"does not match the number of fields expected by the {nameof(PixelFormat)} {pixelFormat} ({compontentCount})");

            for (int i = 0; i < fieldInfos.Length; i++)
            {
                if (fieldInfos[i].FieldType != compontentType)
                    throw new ArgumentException($"The type of field {fieldInfos[i].Name} ({fieldInfos[i].FieldType}) " +
                    $"does not match the field type expected by the {nameof(PixelFormat)} {pixelFormat} ({compontentType.Name})");
            }
        }



        public FrameRenderBuilder CreateRenderTexture(string name, PixelFormat pixelFormat, out RenderTextureRef textureRef)
        {
            textureRef = new RenderTextureRef() { Name = name, Format = pixelFormat };
            _renderTextureSlots.Add(textureRef);

            return this;
        }

        public FrameRenderBuilder RenderPass(string debugName, RenderTextureRef? depthTexture, RenderTextureRef[] colorTextures, FrameRenderInstruction pass)
        {
            _frameBufferSlots.Add(new(depthTexture, colorTextures));

            _instructions.Add((fr, cl) =>
            {
                cl.PushDebugGroup(debugName);
                cl.SetFramebuffer(fr.Framebuffers[_frameBufferSlots.Count - 1]);
                pass(fr, cl);
                cl.PopDebugGroup();
            });

            return this;
        }

        public FrameRenderBuilder Compose(ComputeShader compositionShader, Vector2 size, CompositionResourceSetup setup, PixelFormat pixelFormat, out RenderTextureRef outputTexture)
        {
            CreateRenderTexture("OutputTexture", pixelFormat, out outputTexture);

            _compResourceSlots.Add(setup);

            _instructions.Add((fr, cl) =>
            {
                compositionShader.Dispatch(cl, (uint)Math.Ceiling(size.X / compositionShader.GroupSizeX),
                                               (uint)Math.Ceiling(size.Y / compositionShader.GroupSizeY), 1,
                                               fr.CompResources[_compResourceSlots.Count - 1]);
            });

            return this;
        }

        public FrameRenderer GetFrameRenderer(RenderTextureRef finalOutputTexture)
        {
            return new FrameRenderer(this, finalOutputTexture);
        }


    }
}