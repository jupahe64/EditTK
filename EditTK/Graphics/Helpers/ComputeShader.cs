using EditTK.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using Veldrid;
using Veldrid.SPIRV;

namespace EditTK.Graphics.Helpers
{
    /// <summary>
    /// A compute shader, ready to be executed
    /// <para>see <see cref="Dispatch(CommandList, uint, uint, uint, ResourceSet[])"/></para>
    /// </summary>
    public class ComputeShader : ResourceHolder
    {
        private readonly byte[] _computeShaderBytes;
        private ShaderUniformLayout[] _uniformLayouts;
        private Pipeline? _pipeline;
        private readonly uint _groupSizeX;
        private readonly uint _groupSizeY;
        private readonly uint _groupSizeZ;

        public IReadOnlyList<ShaderUniformLayout> UniformSetLayouts => _uniformLayouts;

        /// <summary>
        /// Creates a new <see cref="GenericModelRenderer{TIndex, TVertex}"/>
        /// </summary>
        /// <param name="computeShaderBytes">The compute shader code in bytes</param>
        /// <param name="uniformLayouts">The layouts of all uniform sets used in the shader(s), 
        /// the order should match the set slot in the shader!</param>
        public ComputeShader(byte[] computeShaderBytes, ShaderUniformLayout[] uniformLayouts, uint groupSizeX, uint groupSizeY, uint groupSizeZ)
        {
            _computeShaderBytes = computeShaderBytes;
            _uniformLayouts = uniformLayouts;
            _groupSizeX = groupSizeX;
            _groupSizeY = groupSizeY;
            _groupSizeZ = groupSizeZ;
        }

        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            Shader shader = factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Compute, _computeShaderBytes, "main", true));


            var resourceLayouts = new ResourceLayout[_uniformLayouts.Length];

            for (int i = 0; i < _uniformLayouts.Length; i++)
            {
                _uniformLayouts[i].EnsureResourcesCreated();

                ResourceLayout? layout = _uniformLayouts[i].ResourceLayout;

                Debug.Assert(layout != null);
                resourceLayouts[i] = layout;
            }

            _pipeline = factory.CreateComputePipeline(new ComputePipelineDescription(
                shader, resourceLayouts, _groupSizeX, _groupSizeY, _groupSizeZ));
        }

        /// <summary>
        /// Executes this <see cref="ComputeShader"/>
        /// </summary>
        /// <param name="cl">The <see cref="CommandList"/> to use for all involved gpu commands</param>
        /// <param name="resourceSets">The resource sets to submit to the shader(s), 
        /// the order should match the set slot in the shader!</param>
        public void Dispatch(CommandList cl, uint groupCountX, uint groupCountY, uint groupCountZ, params ResourceSet[] resourceSets)
        {
            EnsureResourcesCreated();

            cl.SetPipeline(_pipeline);

            for (int i = 0; i < resourceSets.Length; i++)
            {
                cl.SetComputeResourceSet((uint)i, resourceSets[i]);
            }

            cl.Dispatch(groupCountX, groupCountY, groupCountZ);
        }
    }
}
