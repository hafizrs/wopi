using AutoMapper;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Mappers
{
    public class RiqsTaskCockpitSummaryResponseDtoMapperHelper
    {
        public static Mapper InitializeAutoMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RiqsTaskCockpitSummary, RiqsTaskCockpitSummaryDto>();
            });
            var mapper = new Mapper(config);
            return mapper;
        }
    }
}