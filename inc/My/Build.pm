package My::Build;

use strict;
use warnings;
use parent qw(Module::Build);

use File::Basename qw();
use File::Path qw();

=head1 ACTIONS

=cut

=head2 code_perl5

Creates symlinks for perl5 core tests.

=head2 code_p

Creates symlinks for L<Language::P> tests.

=cut

sub _symlink {
    my( $src, $targ ) = @_;
    my $dest = File::Spec->catfile( $targ, File::Basename::basename( $src ) );

    File::Path::mkpath( $targ ) unless -d $targ;
    symlink( $src, $dest );

    return $dest;
}

sub ACTION_code_perl5 {
    my( $self ) = @_;
    my $perl5_path = File::Spec->rel2abs( $self->args( 'perl5' ) );

    if( !-e 't/perl5/t' || !-e 't/harness' ) {
        symlink( File::Spec->catfile( $perl5_path, 't/harness' ), 't/harness' );
        for my $f ( glob( File::Spec->catdir( $perl5_path, 't/base/*.t' ) ) ) {
            _symlink( $f, 't/perl5/t/base' );
        }

        $self->add_to_cleanup( 't/perl5/t', 't/harness' );
    }
}

sub ACTION_code_p {
    my( $self ) = @_;
    my $p_path = File::Spec->rel2abs( $self->args( 'p' ) );

    foreach my $dir ( qw(t/intermediate t/parser t/run t/lib support/toy) ) {
        my $base = File::Spec->catdir( $p_path, $dir );

        foreach my $d ( _all_subdirs( $base ) ) {
            my $rel = File::Spec->abs2rel( $d, $p_path );
            next if $rel eq 't/run/net';

            for my $f ( glob( File::Spec->catfile( $d, '*.t' ) ),
                        glob( File::Spec->catfile( $d, '*.pl' ) ),
                        glob( File::Spec->catfile( $d, '*.pm' ) ) ) {
                my $test = _symlink( $f, $rel );

                $self->add_to_cleanup( $test );
            }
        }
    }
}

=head2 code_dlr

Build the .Net runtime.

=cut

sub _inplace_subst {
    my( $file, $expr ) = @_;

    open my $in,  '<:raw', $file or die "open '$file': $!";
    open my $out, '>:raw', "$file.tmp" or die "open '$file.tmp': $!";

    while( <$in> ) {
        $expr->();
        print $out $_;
    }

    close $in;
    close $out;

    rename "$file.tmp", $file;
}

sub _fix_dlr_path {
    my( $self ) = @_;
    my $dlr = $self->args( 'dlr' );

    $dlr =~ s{/}{\\}g;
    _inplace_subst( 'support/dotnet/dotnet.csproj', sub {
                        s{<HintPath>.*?\\bin\\}{<HintPath>..\\..\\$dlr\\bin\\}i;
                    } );
}

sub ACTION_code_dlr {
    my( $self ) = @_;

    _fix_dlr_path( $self );

    if( !$self->up_to_date( [ 'inc/OpcodesDotNet.pm' ],
                            [ 'support/dotnet/Bytecode/BytecodeGenerated.cs' ] ) ) {
        $self->do_system( $^X, '-Iinc', '-Ilib',
                          '-MOpcodesDotNet', '-e', 'write_dotnet_deserializer()',
                          '--', 'support/dotnet/Bytecode/BytecodeGenerated.cs' );
        $self->add_to_cleanup( 'support/dotnet/Bytecode/BytecodeGenerated.cs' );
    }

    my @files = map glob( "support/dotnet/$_" ), qw(*.cs */*.cs */*/*.cs);

    # only works with MonoDevelop and when mdtool is in path
    if( !$self->up_to_date( [ @files ],
                            [ 'support/dotnet/bin/Debug/dotnet.exe' ] ) ) {
        $self->do_system( 'mdtool', 'build',
                          '--project:dotnet', '--configuration:Debug',
                          'support/dotnet/dotnet.sln' );
    }
}

=head2 build_dlr

Build the Dynamic Language Runtime.

=cut

sub ACTION_build_dlr {
    my( $self ) = @_;

    if( !-d $self->args( 'dlr' ) ) {
        require Archive::Extract;

        File::Path::rmtree( 'extract_tmp' );
        File::Path::mkpath( 'extract_tmp' );

        my $ae = Archive::Extract->new( archive => 'dlr-54115.zip' );

        $ae->extract( to => 'extract_tmp' );

        rename( 'extract_tmp/DLR_Main', $self->args( 'dlr' ) );

        File::Path::rmtree( 'extract_tmp' );
    }

    my $solution = File::Spec->catfile( $self->args( 'dlr' ), 'Solutions',
                                        'Codeplex-DLR.sln' );
    my $metadata_prj = File::Spec->catfile( $self->args( 'dlr' ), 'Runtime',
                                           'Microsoft.Scripting.Metadata',
                                           'Microsoft.Scripting.Metadata.csproj' );
    _inplace_subst( $metadata_prj, sub {
                        s{(<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>)\r?$}
                         {$1<AllowUnsafeBlocks>true</AllowUnsafeBlocks>}g;
                    } );

    foreach my $i ( qw(Microsoft.Scripting.Core Microsoft.Scripting
                       Microsoft.Scripting.Metadata Microsoft.Dynamic) ) {
        my $prj = File::Spec->catfile( $self->args( 'dlr' ), 'Runtime',
                                       $i, "$i.csproj" );

        _inplace_subst( $prj, sub {
                            s{\$\(SolutionDir\)}{..\\}g;
                        } );
        _inplace_subst( $prj, sub {
                            s{(</CodeAnalysisRuleSet>)\r?$}
                             {$1<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>}g;
                        } );

        $self->do_system( 'mdtool', 'build', "--project:$i",
                          '--configuration:v2Debug', $solution );
    }
}

=head2 code

Calls the defult C<code> action, C<code_dlr> if appropriate, and
builds F<lib/Language/P/Opcodes.pm> and F<lib/Language/P/Keywords.pm>
from the files under F<inc>.

=cut

sub ACTION_code {
    my( $self ) = @_;

    $self->depends_on( 'code_dlr' ) if $self->args( 'dlr' );
    $self->depends_on( 'code_perl5' ) if $self->args( 'perl5' );
    $self->depends_on( 'code_p' ) if $self->args( 'p' );

    $self->SUPER::ACTION_code;
}

sub _all_subdirs {
    my( $dir ) = @_;

    return unless -d $dir;

    my @subdirs;

    local $_;

    my $subr = sub {
        return unless -d $File::Find::name;
        push @subdirs, $File::Find::name;
    };

    require File::Find;
    File::Find::find( {wanted => $subr, no_chdir => 1 }, $dir );

    return @subdirs;
}

sub _run_p_tests {
    my( $self, $suffix, @test_dirs ) = @_;

    $self->depends_on( 'code' );

    require TAP::Harness;
    require TAP::Formatter::Console;
    require TAP::Parser::Aggregator;

    my $formatter = TAP::Formatter::Console->new( { jobs => 1 } );
    my $aggregator = TAP::Parser::Aggregator->new;
    $aggregator->start();
    foreach my $test_dir ( @test_dirs ) {
        my( $interpreter, @directories ) = @$test_dir;
        my $harness;

        if( $interpreter ) {
            my $cmdline;
            if( ref $interpreter ) {
                if( $interpreter->[0] =~ /^mono$/ ) {
                    $cmdline = $interpreter;
                } else {
                    $cmdline = [ $self->perl, '-S', '--', @$interpreter ];
                }
            } else {
                $cmdline = [ $self->perl, '-S', '--', $interpreter ];
            }

            $harness = TAP::Harness->new
              ( { formatter => $formatter,
                  exec      => $cmdline,
                  } );
        } else {
            $harness = TAP::Harness->new
              ( { formatter => $formatter,
                  exec      => [ $self->perl, '-Mblib', '--' ],
                  } );
        }

        my @tests = sort map $self->expand_test_dir( $_ ), @directories;

        $_ .= $suffix foreach @tests;

        local $ENV{PERL5OPT} = $ENV{HARNESS_PERL_SWITCHES}
          if $ENV{HARNESS_PERL_SWITCHES};
        $harness->aggregate_tests( $aggregator, @tests );
    }
    $aggregator->stop();
    $formatter->summary( $aggregator );
}

my %test_tags =
  ( 'parser'     => [ [ undef,   _all_subdirs( 't/parser' ) ] ],
    'intermediate' => [ [ undef, _all_subdirs( 't/intermediate' ) ] ],
    'perl5'      => [ [ 'p', _all_subdirs( 't/perl5' ) ] ],
    'run_np'     => [ [ 'p', 't/run', 't/run/net' ] ],
    'run'        => [ [ 'p', _all_subdirs( 't/run' ) ] ],
    'all'        => [ 'parser', 'intermediate', 'run', 'perl5' ],
    );

=head2 test_parser

Runs the tests under F<t/parser>.

=head2 test_intermediate

Runs the tests under F<t/intermediate>.

=head2 test_run

Runs the tests under F<t/run> using F<p>.

=head2 test_perl5

Runs the tests under F<t/perl5> using F<p>.

=cut

sub ACTION_test_parser;
sub ACTION_test_intermediate;
sub ACTION_test_run;
sub ACTION_test_perl5;

sub _expand_tags {
    my( $self, $tag ) = @_;
    die "Unknown test tag '$tag'" unless exists $test_tags{$tag};

    my $base = $test_tags{$tag};
    my @res;

    foreach my $part ( @$base ) {
        if( ref $part ) {
            push @res, $part;
        } else {
            push @res, _expand_tags( $self, $part );
        }
    }

    return @res;
}

sub _run_dotnet {
    my( $self, $suffix, $args, @tags ) = @_;

    my @run;
    foreach my $tag ( @tags ) {
        my( $interpreter, @directories ) = @$tag;

        push @run, [ [ 'mono', 'support/dotnet/bin/Debug/dotnet.exe', @$args ],
                     @directories ];
    }

    $self->_run_p_tests( $suffix, @run );
}

sub _byte_compile {
    my( $self, @tags ) = @_;

    my @byte_compile;
    foreach my $tag ( @tags ) {
        my( $interpreter, @directories ) = @$tag;

        push @byte_compile, [ [ $interpreter, '-Zdump-bytecode' ],
                              @directories ];
    }

    local $ENV{P_BYTECODE_PATH} = 'support/bytecode';
    $self->_run_p_tests( '', @byte_compile );
}

sub ACTION_test_dotnet_run {
    my( $self ) = @_;

    $self->_run_dotnet( '.pb', [ '-Znative-regex' ],
                        _expand_tags( $self, 'run_np' ) );
}

sub ACTION_test_dump_bytecode {
    my( $self ) = @_;

    $self->_byte_compile( _expand_tags( $self, 'run' ) );
}

=head2 test

Runs all the tests (uses th Toy runtime for the tests that require
running code).

=cut

sub ACTION_test {
    my( $self ) = @_;

    $self->_run_p_tests( '', _expand_tags( $self, 'all' ) );
}

our $AUTOLOAD;
sub AUTOLOAD {
    ( my $function = $AUTOLOAD ) =~ s/^.*:://;

    return if $function eq 'DESTROY';
    die "Unknown action '$function'"
        unless $function =~ /^ACTION_test_(\w+)/;

    $_[0]->_run_p_tests( '', _expand_tags( $_[0], $1 ) );
}

1;
