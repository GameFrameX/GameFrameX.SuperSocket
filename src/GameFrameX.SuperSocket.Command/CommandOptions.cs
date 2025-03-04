using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace GameFrameX.SuperSocket.Command
{
    /// <summary>
    /// 命令选项类，用于管理和配置命令源
    /// </summary>
    public class CommandOptions : ICommandSource
    {
        /// <summary>
        /// 初始化命令选项类的新实例
        /// </summary>
        public CommandOptions()
        {
            CommandSources = new List<ICommandSource>();
            _globalCommandFilterTypes = new List<Type>();
        }

        /// <summary>
        /// 获取或设置程序集配置数组
        /// </summary>
        public CommandAssemblyConfig[] Assemblies { get; set; }

        /// <summary>
        /// 获取或设置命令源列表
        /// </summary>
        public List<ICommandSource> CommandSources { get; set; }

        private List<Type> _globalCommandFilterTypes;

        /// <summary>
        /// 获取全局命令过滤器类型列表
        /// </summary>
        public IReadOnlyList<Type> GlobalCommandFilterTypes
        {
            get { return _globalCommandFilterTypes; }
        }

        /// <summary>
        /// 根据指定条件获取命令类型
        /// </summary>
        /// <param name="criteria">用于筛选命令类型的条件</param>
        /// <returns>符合条件的命令类型集合</returns>
        public IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria)
        {
            var commandSources = CommandSources;
            var configuredAssemblies = Assemblies;

            if (configuredAssemblies != null && configuredAssemblies.Any())
            {
                commandSources.AddRange(configuredAssemblies);
            }

            var commandTypes = new List<Type>();

            foreach (var source in commandSources)
            {
                commandTypes.AddRange(source.GetCommandTypes(criteria));
            }

            return commandTypes;
        }

        /// <summary>
        /// 添加全局命令过滤器类型
        /// </summary>
        /// <param name="commandFilterType">要添加的命令过滤器类型</param>
        internal void AddGlobalCommandFilterType(Type commandFilterType)
        {
            _globalCommandFilterTypes.Add(commandFilterType);
        }
    }

    /// <summary>
    /// 命令程序集配置类
    /// </summary>
    public class CommandAssemblyConfig : AssemblyBaseCommandSource, ICommandSource
    {
        /// <summary>
        /// 获取或设置程序集名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 根据指定条件获取命令类型
        /// </summary>
        /// <param name="criteria">用于筛选命令类型的条件</param>
        /// <returns>符合条件的命令类型集合</returns>
        public IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria)
        {
            return GetCommandTypesFromAssembly(Assembly.Load(Name)).Where(t => criteria(t));
        }
    }

    /// <summary>
    /// 实际命令程序集类
    /// </summary>
    public class ActualCommandAssembly : AssemblyBaseCommandSource, ICommandSource
    {
        /// <summary>
        /// 获取或设置程序集
        /// </summary>
        public Assembly Assembly { get; set; }

        /// <summary>
        /// 根据指定条件获取命令类型
        /// </summary>
        /// <param name="criteria">用于筛选命令类型的条件</param>
        /// <returns>符合条件的命令类型集合</returns>
        public IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria)
        {
            return GetCommandTypesFromAssembly(Assembly).Where(t => criteria(t));
        }
    }

    /// <summary>
    /// 程序集基础命令源抽象类
    /// </summary>
    public abstract class AssemblyBaseCommandSource
    {
        /// <summary>
        /// 从程序集中获取命令类型
        /// </summary>
        /// <param name="assembly">要处理的程序集</param>
        /// <returns>程序集中的类型集合</returns>
        public IEnumerable<Type> GetCommandTypesFromAssembly(Assembly assembly)
        {
            return assembly.GetExportedTypes();
        }
    }

    /// <summary>
    /// 实际命令类
    /// </summary>
    public class ActualCommand : ICommandSource
    {
        /// <summary>
        /// 获取或设置命令类型
        /// </summary>
        public Type CommandType { get; set; }

        /// <summary>
        /// 根据指定条件获取命令类型
        /// </summary>
        /// <param name="criteria">用于筛选命令类型的条件</param>
        /// <returns>符合条件的命令类型集合</returns>
        public IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria)
        {
            if (criteria(CommandType))
                yield return CommandType;
        }
    }

    /// <summary>
    /// 命令选项扩展方法类
    /// </summary>
    public static class CommandOptionsExtensions
    {
        /// <summary>
        /// 添加命令到命令选项
        /// </summary>
        /// <typeparam name="TCommand">要添加的命令类型</typeparam>
        /// <param name="commandOptions">命令选项实例</param>
        public static void AddCommand<TCommand>(this CommandOptions commandOptions)
        {
            commandOptions.CommandSources.Add(new ActualCommand { CommandType = typeof(TCommand) });
        }

        /// <summary>
        /// 添加命令到命令选项
        /// </summary>
        /// <param name="commandOptions">命令选项实例</param>
        /// <param name="commandType">要添加的命令类型</param>
        public static void AddCommand(this CommandOptions commandOptions, Type commandType)
        {
            commandOptions.CommandSources.Add(new ActualCommand { CommandType = commandType });
        }

        /// <summary>
        /// 添加命令程序集到命令选项
        /// </summary>
        /// <param name="commandOptions">命令选项实例</param>
        /// <param name="commandAssembly">要添加的命令程序集</param>
        public static void AddCommandAssembly(this CommandOptions commandOptions, Assembly commandAssembly)
        {
            commandOptions.CommandSources.Add(new ActualCommandAssembly { Assembly = commandAssembly });
        }

        /// <summary>
        /// 添加全局命令过滤器
        /// </summary>
        /// <typeparam name="TCommandFilter">要添加的命令过滤器类型</typeparam>
        /// <param name="commandOptions">命令选项实例</param>
        public static void AddGlobalCommandFilter<TCommandFilter>(this CommandOptions commandOptions)
            where TCommandFilter : CommandFilterBaseAttribute
        {
            commandOptions.AddGlobalCommandFilterType(typeof(TCommandFilter));
        }

        /// <summary>
        /// 添加全局命令过滤器
        /// </summary>
        /// <param name="commandOptions">命令选项实例</param>
        /// <param name="commandFilterType">要添加的命令过滤器类型</param>
        public static void AddGlobalCommandFilter(this CommandOptions commandOptions, Type commandFilterType)
        {
            if (!typeof(CommandFilterBaseAttribute).IsAssignableFrom(commandFilterType))
                throw new Exception("The command filter type must inherit CommandFilterBaseAttribute.");

            commandOptions.AddGlobalCommandFilterType(commandFilterType);
        }
    }
}