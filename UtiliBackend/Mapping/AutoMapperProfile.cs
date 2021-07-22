﻿using System.Xml;
using AutoMapper;
using Discord.Rest;
using NewDatabase.Entities;
using UtiliBackend.Models;

namespace UtiliBackend.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<RestTextChannel, TextChannelModel>();
            
            CreateMap<RestVoiceChannel, VoiceChannelModel>();
            
            CreateMap<RestRole, RoleModel>();

            CreateMap<CoreConfiguration, CoreConfigurationModel>();
            
            CreateMap<AutopurgeConfiguration, AutopurgeConfigurationModel>()
                .ForMember(
                    dest => dest.Timespan, 
                    opt => opt.MapFrom(s => XmlConvert.ToString(s.Timespan)));
            
            CreateMap<ChannelMirroringConfiguration, ChannelMirroringConfigurationModel>();

            CreateMap<InactiveRoleConfiguration, InactiveRoleConfigurationModel>()
                .ForMember(
                    dest => dest.Threshold,
                    opt => opt.MapFrom(s => XmlConvert.ToString(s.Threshold)))
                .ForMember(
                    dest => dest.AutoKickThreshold,
                    opt => opt.MapFrom(s => XmlConvert.ToString(s.AutoKickThreshold)));

            CreateMap<PremiumSlot, PremiumSlotModel>();
            
            CreateMap<Subscription, SubscriptionModel>()
                .ForMember(
                    dest => dest.ExpiresAt, 
                    opt => opt.MapFrom(s => XmlConvert.ToString(s.ExpiresAt, XmlDateTimeSerializationMode.Utc)));
        }
    }
}
