package TestBenchmark;

use strict;
use warnings;

use Dumbbench;

our @EXPORT_OK = qw(run_parse run_bytecode);
our %EXPORT_TAGS =
  ( all => \@EXPORT_OK,
    );

our @p = qw(mono support/dotnet/bin/Debug/dotnet.exe);

sub run_bytecode_instance {
    my( $pb ) = @_;

    return Dumbbench::Instance::Cmd->new
               ( command => [ @p, $pb ],
                 dry_run_command => [ @p, '-c', $pb ],
                 );
}

sub run_parse_instance {
    my( $pl ) = @_;

    return Dumbbench::Instance::Cmd->new
               ( command => [ @p, '-c', $pl ],
                 dry_run_command => [ @p, '-e', '1' ],
                 );
}

sub run_bytecode {
    my( $bench, $pb ) = @_;

    $bench->add_instances( run_bytecode_instance( $pb ) );
}

sub run_parse {
    my( $bench, $pl ) = @_;

    $bench->add_instances( run_parse_instance( $pl ) );
}

package t::lib::TestBenchmark;

sub import {
    shift;

    strict->import;
    warnings->import;
    Test::More->import( @_ );
    Exporter::export( 'TestBenchmark', scalar caller, ':all' );
}

1;
