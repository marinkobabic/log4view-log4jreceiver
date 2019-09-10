## Log4View log4jreceiver

Actually Log4View is not supporting log4j2 TCP

```yaml
Configuration:
  Appenders:
    Console:
      name: Console_Appender
      target: SYSTEM_OUT
      PatternLayout:
        Pattern: "%d %-5p %C{3} (%F:%L) - %m%n"
    Socket:
      name: Socket_Appender
      host: 127.0.0.1
      port: 4447
      protocol: TCP
      XMLLayout:
        complete: true
  Loggers:
    Root:
      level: info
      AppenderRef:
      - ref: Socket_Appender
      - ref: Console_Appender
```

Therefore this plugin has been created with the hope to get it as part of the following Log4View versions.

### How to install the plugin
Download the Log4View.Log4jReceiver.dll from the releases and copy the assembly to the programs directory like in this example "C:\Program Files (x86)\Log4View V2\Plugins". Log4View will during the startup load automatically the plugins located in the plugin directory.

### Use the plugin
After the plugin was loaded you can find a new menu item in the Log4View receiver menu
<image src="select_menu.png"/>

