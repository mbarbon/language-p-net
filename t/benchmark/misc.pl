#!/usr/bin/perl -w

use Dumbbench;

our @p = qw(mono support/dotnet/bin/Debug/dotnet.exe);

sub _run_pb {
    my( $pb ) = @_;

    return Dumbbench::Instance::Cmd->new
               ( command => [ @p, $pb ],
                 dry_run_command => [ @p, '-c', $pb ],
                 );
}

sub _parse {
    my( $pl ) = @_;

    return Dumbbench::Instance::Cmd->new
               ( command => [ @p, '-c', $pl ],
                 dry_run_command => [ @p, '-e', '1' ],
                 );
}

my $bench = Dumbbench->new( initial_runs => 10 );
$bench->add_instances( _run_pb( 't/benchmark/tests/rec.pl.pb' ) );
$bench->add_instances( _run_pb( 't/benchmark/tests/iter.pl.pb' ) );
$bench->add_instances( _parse( 't/benchmark/tests/iter.pl' ) );

$bench->run;
$bench->report;
