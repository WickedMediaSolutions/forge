$mainPath = 'C:\Users\mcdor\MapMaker\EvenniaAtlas\MainWindow.xaml'
$content = Get-Content $mainPath -Raw

$newToolbar = @"
        <!-- HEADER + TOOLBAR -->
        <StackPanel Grid.Row="0">
            <!-- Branding strip -->
            <Border Background="{StaticResource ShellHeaderBg}" Padding="8,3">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="3"/>
                        <ColumnDefinition Width="8"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Border Grid.Column="0" Background="{StaticResource EvenniaGreen}" CornerRadius="1" VerticalAlignment="Stretch"/>
                    <StackPanel Grid.Column="2" Orientation="Horizontal">
                        <TextBlock Text="EVENNIA ATLAS" FontSize="13" FontWeight="SemiBold" Foreground="#E0E0E0" VerticalAlignment="Center"/>
                        <TextBlock Text="  —  " Foreground="#555555" FontSize="11" VerticalAlignment="Center"/>
                        <TextBlock Text="Visual World &amp; Map Editor for Evennia" FontSize="10" Foreground="#808080" VerticalAlignment="Center"/>
                    </StackPanel>
                </Grid>
            </Border>
            <!-- Toolbar strip -->
            <Border Background="{StaticResource ShellToolbarBg}" Padding="6,3" BorderBrush="{StaticResource ShellToolbarBorder}" BorderThickness="0,1,0,0">
                <StackPanel Orientation="Horizontal">
                    <!-- Menu -->
                    <Button Content="Menu" Style="{StaticResource ToolbarButtonStyle}" Click="Menu_Click"/>
                    <Rectangle Style="{StaticResource ToolbarSeparatorStyle}"/>
                    <!-- File operations -->
                    <Button Content="New" Style="{StaticResource ToolbarButtonStyle}" Click="New_Click"/>
                    <Button Content="Open" Style="{StaticResource ToolbarButtonStyle}" Click="Open_Click"/>
                    <Button Content="Save" Style="{StaticResource ToolbarButtonStyle}" Click="Save_Click"/>
                    <Button Content="Save As" Style="{StaticResource ToolbarButtonStyle}" Click="SaveAs_Click"/>
                    <Rectangle Style="{StaticResource ToolbarSeparatorStyle}"/>
                    <!-- Undo/Redo -->
                    <Button Content="Undo" Style="{StaticResource ToolbarButtonStyle}" Click="Undo_Click"/>
                    <Button Content="Redo" Style="{StaticResource ToolbarButtonStyle}" Click="Redo_Click"/>
                    <Rectangle Style="{StaticResource ToolbarSeparatorStyle}"/>
                    <!-- Build / Auto Reverse with LEDs -->
                    <Button x:Name="BuildBtn" Click="BuildMode_Click" Style="{StaticResource ToolbarButtonStyle}"
                            ToolTip="{Binding BuildModeText}">
                        <Button.Content>
                            <StackPanel Orientation="Horizontal">
                                <Ellipse Width="8" Height="8" Margin="0,0,5,0">
                                    <Ellipse.Style>
                                        <Style TargetType="Ellipse">
                                            <Setter Property="Fill" Value="{StaticResource LedOffGray}"/>
                                            <Style.Triggers>
                                                <DataTrigger Binding="{Binding BuildMode}" Value="True">
                                                    <Setter Property="Fill" Value="{StaticResource LedOnGlow}"/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </Ellipse.Style>
                                </Ellipse>
                                <TextBlock Text="BUILD" VerticalAlignment="Center"/>
                            </StackPanel>
                        </Button.Content>
                    </Button>
                    <Button x:Name="AutoRevBtn" Click="AutoReverse_Click" Style="{StaticResource ToolbarButtonStyle}"
                            ToolTip="{Binding AutoReverseText}">
                        <Button.Content>
                            <StackPanel Orientation="Horizontal">
                                <Ellipse Width="8" Height="8" Margin="0,0,5,0">
                                    <Ellipse.Style>
                                        <Style TargetType="Ellipse">
                                            <Setter Property="Fill" Value="{StaticResource LedOffGray}"/>
                                            <Style.Triggers>
                                                <DataTrigger Binding="{Binding AutoReverse}" Value="True">
                                                    <Setter Property="Fill" Value="{StaticResource LedOnGlow}"/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </Ellipse.Style>
                                </Ellipse>
                                <TextBlock Text="AUTO REVERSE" VerticalAlignment="Center"/>
                            </StackPanel>
                        </Button.Content>
                    </Button>
                    <Rectangle Style="{StaticResource ToolbarSeparatorStyle}"/>
                    <!-- Floor controls group -->
                    <Border Background="{StaticResource ShellGroupBg}" CornerRadius="2" Padding="1,0" Margin="0,0,0,0">
                        <StackPanel Orientation="Horizontal">
                            <Button Content="▼" Style="{StaticResource ToolbarButtonStyle}" Click="FloorDown_Click"
                                    FontSize="8" Padding="6,3" ToolTip="Floor Down"/>
                            <TextBlock Text="{Binding FloorLabel}" Foreground="#89D185" FontWeight="SemiBold"
                                       VerticalAlignment="Center" Margin="4,0" FontSize="11" MinWidth="32"
                                       TextAlignment="Center"/>
                            <Button Content="▲" Style="{StaticResource ToolbarButtonStyle}" Click="FloorUp_Click"
                                    FontSize="8" Padding="6,3" ToolTip="Floor Up"/>
                        </StackPanel>
                    </Border>
                    <Rectangle Style="{StaticResource ToolbarSeparatorStyle}"/>
                    <!-- Delete -->
                    <Button Content="Delete" Style="{StaticResource ToolbarButtonStyle}" Click="Delete_Click"
                            Foreground="#CC7777"/>
                    <Rectangle Style="{StaticResource ToolbarSeparatorStyle}"/>
                    <!-- View controls -->
                    <Button Content="Fit" Style="{StaticResource ToolbarButtonStyle}" Click="Fit_Click"
                            ToolTip="Fit all rooms on this floor into view"/>
                    <Button Content="Center" Style="{StaticResource ToolbarButtonStyle}" Click="Center_Click"
                            ToolTip="Center on selected room"/>
                    <Rectangle Style="{StaticResource ToolbarSeparatorStyle}"/>
                    <!-- Validate / Export -->
                    <Button Content="Validate" Style="{StaticResource ToolbarButtonStyle}" Click="Validate_Click"/>
                    <Button Content="Export" Style="{StaticResource ToolbarButtonStyle}" Click="Export_Click"/>
                    <Rectangle Style="{StaticResource ToolbarSeparatorStyle}"/>
                    <!-- Zoom controls group -->
                    <Border Background="{StaticResource ShellGroupBg}" CornerRadius="2" Padding="1,0">
                        <StackPanel Orientation="Horizontal">
                            <Button Content="-" Style="{StaticResource ToolbarButtonStyle}" Click="ZoomOut_Click"
                                    FontSize="14" FontWeight="Bold" Padding="5,2"
                                    ToolTip="Zoom Out (Ctrl+MouseWheel Down)"/>
                            <TextBlock Text="{Binding ZoomPercentage}" Foreground="#75BEFF"
                                       FontWeight="SemiBold" VerticalAlignment="Center"
                                       Margin="3,0" FontSize="11" MinWidth="36"
                                       TextAlignment="Center"/>
                            <Button Content="+" Style="{StaticResource ToolbarButtonStyle}" Click="ZoomIn_Click"
                                    FontSize="14" FontWeight="Bold" Padding="5,2"
                                    ToolTip="Zoom In (Ctrl+MouseWheel Up)"/>
                        </StackPanel>
                    </Border>
                </StackPanel>
            </Border>
        </StackPanel>
"@

$pattern = '(?s)<!-- TOOLBAR -->.*?</ToolBarTray>'
$newContent = $content -replace $pattern, $newToolbar
Set-Content $mainPath -Value $newContent -Encoding UTF8
Write-Host 'MainWindow.xaml updated'
