package OpcodesDotNet;

use strict;
use warnings;
use Exporter 'import';

our @EXPORT = qw(write_dotnet_deserializer write_bytecode_factory
                 write_bytecode_classes);

use Language::P::Parser::OpcodeList;

sub write_dotnet_deserializer {
    my( $file ) = @ARGV;

    my %op = %{Language::P::Parser::OpcodeList::parse_opdesc()};

    open my $out, '>', $file;

    print $out <<'EOT';
using System.IO;

namespace org.mbarbon.p.runtime
{
    public partial class Serializer
    {
        public static Opcode ReadOpcode(BinaryReader reader, Subroutine[] subroutines, Subroutine sub)
        {
            var num = (Opcode.OpNumber)reader.ReadInt16();
            string file;
            int line;
            Opcode op;

            ReadPos(reader, out file, out line);

            switch (num)
            {
EOT

    OPCODES: while( my( $k, $v ) = each %op ) {
        my( $attrs, $class ) = ( $v->[3][0], $v->[5] );
        next unless @$attrs;

        if( !$class ) {
            for( my $i = 0; $i < @$attrs; $i += 2) {
                if(    $attrs->[$i] ne 'context'
                    && $attrs->[$i] ne 'arg_count' ) {
                    print $out sprintf <<'EOT', $k, $class, $class;
            case Opcode.OpNumber.%s:
                throw new System.Exception(string.Format("Unhandled opcode {0:S} in deserialization", num.ToString()));
EOT
                    next OPCODES;
                }
            }

            $class = 'Opcode';
        }

        print $out sprintf <<'EOT', $k;
            case Opcode.OpNumber.%s:
            {
EOT

        if( $class ) {
            print $out sprintf <<'EOT', $class, $class;
                %s opc = new %s();
                op = opc;
EOT
        }

        for( my $i = 0; $i < @$attrs; $i += 2 ) {
            my $type = $attrs->[$i + 1];
            my $name = $attrs->[$i];
            next if $name eq 'arg_count';
            my $n = join '', map ucfirst, split /_/, $name;
            if( $type eq 's' ) {
                print $out sprintf <<'EOT', $n;
                opc.%s = ReadString(reader);
EOT
            } elsif( $type eq 'i' || $type eq 'i4' ) {
                print $out sprintf <<'EOT', $n;
                opc.%s = reader.ReadInt32();
EOT
            } elsif( $type eq 'f' ) {
                print $out sprintf <<'EOT', $n;
                opc.%s = reader.ReadDouble();
EOT
            } elsif( $type eq 'i_sigil' ) {
                print $out sprintf <<'EOT', $n;
                opc.%s = (Opcode.Sigil)reader.ReadByte();
EOT
            } elsif( $type eq 'i1' ) {
                print $out sprintf <<'EOT', $n;
                opc.%s = reader.ReadByte();
EOT
            } elsif( $type eq 'b' ) {
                print $out sprintf <<'EOT', $n, $n, $n;
                int %s = reader.ReadInt32();
                opc.%s = sub.BasicBlocks[%s];
EOT
            } elsif( $type eq 'c' ) {
                print $out sprintf <<'EOT', $n, $n, $n;
                int %s = reader.ReadInt32();
                opc.%s = subroutines[%s];
EOT
            } elsif( $type eq 'ls' || $type eq 'lp' ) {
                print $out sprintf <<'EOT';
                opc.LexicalInfo = sub.Lexicals[reader.ReadInt32()];
EOT
            }
        }

        print $out <<'EOT';
                break;
            }
EOT
    }

    print $out <<'EOT';
            default:
            {
                op = new Opcode();
                break;
            }
            }

            op.Number = num;
            op.Position.File = file;
            op.Position.Line = line;
            int count = reader.ReadInt32();
            op.Childs = new Opcode[count];

            for (int i = 0; i < count; ++i)
            {
                op.Childs[i] = ReadOpcode(reader, subroutines, sub);
            }

            return op;
        }
    }

    public partial class Opcode
    {
        public enum OpNumber : short
        {
EOT

    my $index = 1;
    foreach my $k ( sort keys %op ) {
        print $out <<EOT;
            $k = $index,
EOT
        ++$index;
    }

    print $out <<'EOT';
        }
    }
}
EOT
}

sub write_bytecode_classes {
    my( $file ) = @ARGV;

    my %op = %{Language::P::Parser::OpcodeList::parse_opdesc()};
    my %classes = %{Language::P::Parser::OpcodeList::group_opcode_numbers( \%op )};
    my %attributes = %{Language::P::Parser::OpcodeList::group_opcode_attributes( \%op )};

    open my $out, '>', $file;

    print $out <<'EOT';
using org.mbarbon.p.values;

namespace org.mbarbon.p.runtime
{
EOT

    while( my( $class, $ops ) = each %classes ) {
        my $attrs = $attributes{$class};

        printf $out <<'EOT', $class;
    public partial class %s
    {
EOT

        for( my $i = 0; $i < @$attrs; $i += 2 ) {
            my $type = $attrs->[$i + 1];
            my $name = $attrs->[$i];
            next if $name eq 'arg_count' || $name eq 'context';
            next if    $class eq 'RegexReplace'
                    && ( $name eq 'index' || $name eq 'flags' );
            my $n = join '', map ucfirst, split /_/, $name;
            my $ctype;

            if( $type eq 's' ) {
                $ctype = 'string';
            } elsif( $type eq 'i' || $type eq 'i4' || $type eq 'i1' ) {
                $ctype = 'int';
            } elsif( $type eq 'i_a' ) {
                $ctype = 'int[]';
            } elsif( $type eq 'f' ) {
                $ctype = 'double';
            } elsif( $type eq 'i_sigil' ) {
                $ctype = 'Opcode.Sigil';
            } elsif( $type eq 'i_sigil_a' ) {
                $ctype = 'Opcode.Sigil[]';
            } elsif( $type eq 'b' ) {
                $ctype = 'BasicBlock';
            } elsif( $type eq 'b_a' ) {
                $ctype = 'BasicBlock[]';
            } elsif( $type eq 'c' ) {
                $ctype = 'Subroutine';
            } elsif( $type eq 'ls' || $type eq 'lp' ) {
                # skipped for now
                next;
            } else {
                die "Unhandled type '$type'";
            }

            printf $out <<'EOT', $ctype, $name, $n;
        public %s %s() { return %s; }
EOT
        }

        print $out <<'EOT';
    }

EOT
    }

    print $out <<'EOT';
}
EOT
}

sub write_bytecode_factory {
    my( $file ) = @ARGV;

    my %op = %{Language::P::Parser::OpcodeList::parse_opdesc()};
    my %classes = %{Language::P::Parser::OpcodeList::group_opcode_numbers( \%op )};
    my %attributes = %{Language::P::Parser::OpcodeList::group_opcode_attributes( \%op )};

    open my $out, '>', $file;

    print $out <<'EOT';
using org.mbarbon.p.values;

namespace org.mbarbon.p.runtime
{
    public partial class Opcode
    {
        public static IP5Any WrapCreate(Runtime runtime, Opcode.ContextValues context,
                                        P5ScratchPad pad, P5Array args)
        {
            var arg = args.GetItem(runtime, 0).DereferenceHash(runtime);
            var opcode_n = (OpNumber)arg.GetItem(runtime, "opcode_n").AsInteger(runtime);
            var attrs = arg.ExistsKey(runtime, "attributes") ?
                            arg.GetItem(runtime, "attributes").DereferenceHash(runtime) :
                            null;
            Opcode op;

            switch (opcode_n)
            {
EOT

    while( my( $class, $ops ) = each %classes ) {
        for my $op ( @$ops ) {
            printf $out <<'EOT',
            case OpNumber.%s:
EOT
              $op;

            printf $out <<'EOT',
            {
                var opc = new %s();
                op = opc;
EOT
              $class;

            my $attrs = $attributes{$class};
            for( my $i = 0; $i < @$attrs; $i += 2 ) {
                my $type = $attrs->[$i + 1];
                my $name = $attrs->[$i];
                next if $name eq 'arg_count';
                my $n = join '', map ucfirst, split /_/, $name;
                if( $name eq 'to' && ( $class eq 'CondJump' || $class eq 'RegexQuantifier' ) ) {
                    print $out sprintf <<'EOT';
                if (attrs.ExistsKey(runtime, "to"))
                {
                    opc.To = (BasicBlock)NetGlue.UnwrapValue(attrs.GetItem(runtime, "to"),
                                                             typeof(BasicBlock));
                }
                else
                {
                    opc.True = (BasicBlock)NetGlue.UnwrapValue(attrs.GetItem(runtime, "true"),
                                                               typeof(BasicBlock));
                    opc.False = (BasicBlock)NetGlue.UnwrapValue(attrs.GetItem(runtime, "false"),
                                                                typeof(BasicBlock));
                }
EOT

                } elsif( $type eq 's' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = attrs.GetItem(runtime, "%s").AsString(runtime);
EOT
                } elsif( $type eq 'i' || $type eq 'i4' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = attrs.GetItem(runtime, "%s").AsInteger(runtime);
EOT
                } elsif( $type eq 'f' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = attrs.GetItem(runtime, "%s").AsFloat(runtime);
EOT
                } elsif( $type eq 'i_sigil' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = (Opcode.Sigil)attrs.GetItem(runtime, "%s").AsInteger(runtime);
EOT
                } elsif( $type eq 'i1' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = (byte)attrs.GetItem(runtime, "%s").AsInteger(runtime);
EOT
                } elsif( $type eq 'b' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = (BasicBlock)NetGlue.UnwrapValue(attrs.GetItem(runtime, "%s"), typeof(BasicBlock));
EOT
                } elsif( $type eq 'c' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = (Subroutine)NetGlue.UnwrapValue(attrs.GetItem(runtime, "%s"), typeof(Subroutine));
EOT
                } elsif( $type eq 'ls' || $type eq 'lp' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = (LexicalInfo)NetGlue.UnwrapValue(attrs.GetItem(runtime, "%s"), typeof(LexicalInfo));
EOT
                } elsif( $type eq 'i_sigil_a' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = NetGlue.UnwrapArray<Opcode.Sigil>(runtime, attrs.GetItem(runtime, "%s"));
EOT
                } elsif( $type eq 'i_a' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = NetGlue.UnwrapArray<int>(runtime, attrs.GetItem(runtime, "%s"));
EOT
                } elsif( $type eq 'b_a' ) {
                    print $out sprintf <<'EOT', $n, $name;
                opc.%s = NetGlue.UnwrapArray<BasicBlock>(runtime, attrs.GetItem(runtime, "%s"));
EOT
                } else {
                    die "Unhandled type '$type'";
                }
            }

            print $out <<'EOT';
                break;
            }
EOT

        }
    }

    print $out <<'EOT';
            default:
            {
                op = new Opcode();
                if (attrs != null && attrs.ExistsKey(runtime, "context"))
                    op.Context = (byte)attrs.GetItem(runtime, "context").AsInteger(runtime);
                break;
            }
            }

            // op number
            op.Number = opcode_n;

            // pos
            if (arg.ExistsKey(runtime, "pos"))
            {
                var p = arg.GetItem(runtime, "pos") as P5Scalar;
                var w = p.NetWrapper(runtime);

                if (w != null)
                    op.Position = (Position)NetGlue.UnwrapValue(p, typeof(Position));
                else if (p.IsDefined(runtime))
                {
                    var pos = p.DereferenceArray(runtime);

                    op.Position.File = pos.GetItem(runtime, 0).AsString(runtime);
                    op.Position.Line = pos.GetItem(runtime, 1).AsInteger(runtime);
                }
            }

            // parameters
            if (arg.ExistsKey(runtime, "parameters"))
            {
                var parms = arg.GetItem(runtime, "parameters").DereferenceArray(runtime);
                var count = parms.GetCount(runtime);

                op.Childs = new Opcode[count];

                for (int i = 0; i < count; ++i)
                    op.Childs[i] = NetGlue.UnwrapValue(parms.GetItem(runtime, i), typeof(Opcode)) as Opcode;
            }

            return NetGlue.WrapValue(op);
        }
    }
}
EOT
}

1;
