namespace GameFrameX.SuperSocket.ProtoBase.ProxyProtocol
{
    /// <summary>
    /// A factory for creating pipeline filters that handle proxy protocol packages.
    /// </summary>
    /// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
    public class ProxyProtocolPipelineFilterFactory<TPackageInfo> : IPipelineFilterFactory<TPackageInfo>
    {
        /// <summary>
        /// The underlying pipeline filter factory used to create application-specific filters.
        /// </summary>
        private readonly IPipelineFilterFactory<TPackageInfo> _pipelineFilterFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyProtocolPipelineFilterFactory{TPackageInfo}"/> class.
        /// </summary>
        /// <param name="pipelineFilterFactory">The underlying pipeline filter factory.</param>
        public ProxyProtocolPipelineFilterFactory(IPipelineFilterFactory<TPackageInfo> pipelineFilterFactory)
        {
            _pipelineFilterFactory = pipelineFilterFactory;
        }

        /// <summary>
        /// Creates a pipeline filter for the specified client.
        /// </summary>
        /// <param name="client">The client for which the pipeline filter is created.</param>
        /// <returns>The created pipeline filter.</returns>
        public IPipelineFilter<TPackageInfo> Create(object client)
        {
            return new ProxyProtocolPipelineFilter<TPackageInfo>(_pipelineFilterFactory.Create(client));
        }
    }
}